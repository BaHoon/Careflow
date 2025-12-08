using System.Linq;
using System.Text.Json;
using CareFlow.Core.Enums;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Application.Services;

/// <summary>
/// 根据手术医嘱生成执行任务的简单工厂，后续可被 API 或 Seeder 调用。
/// </summary>
public interface IExecutionTaskFactory
{
    IReadOnlyList<ExecutionTask> CreateTasks(MedicalOrder medicalOrder);
}

/// <summary>
/// 当前实现仅支持手术类医嘱，将“术前准备”拆成可逐项勾选的任务。
/// </summary>
public sealed class SurgicalExecutionTaskFactory : IExecutionTaskFactory
{
    private static readonly IReadOnlyList<PreOpStepDescriptor> Steps = new List<PreOpStepDescriptor>
    {
        new("PRE_EDU", "术前宣教确认", TimeSpan.FromHours(-16), BuildEducationPayload),
        new("PRE_NURSING", "护理操作和管路准备", TimeSpan.FromHours(-12), BuildNursingPayload),
        new("PRE_SUPPLY", "手术带入物品核对", TimeSpan.FromHours(-6), BuildSupplyPayload)
    };

    public IReadOnlyList<ExecutionTask> CreateTasks(MedicalOrder medicalOrder)
    {
        if (medicalOrder is not SurgicalOrder surgicalOrder)
        {
            throw new ArgumentException("当前工厂仅支持手术医嘱", nameof(medicalOrder));
        }

        var result = new List<ExecutionTask>(Steps.Count);
        foreach (var step in Steps)
        {
            result.Add(new ExecutionTask
            {
                MedicalOrderId = surgicalOrder.Id,
                MedicalOrder = surgicalOrder,
                PatientId = surgicalOrder.PatientId,
                Patient = surgicalOrder.Patient,
                PlannedStartTime = surgicalOrder.ScheduleTime.Add(step.PlannedOffset),
                Status = ExecutionTaskStatus.Pending.ToString(),
                DataPayload = JsonSerializer.Serialize(new
                {
                    step.StepCode,
                    step.StepName,
                    stepPayload = step.PayloadBuilder(surgicalOrder)
                })
            });
        }

        return result;
    }

    private static object BuildEducationPayload(SurgicalOrder order)
    {
        var instructions = new List<string>
        {
            $"向患者说明手术：{order.SurgeryName}",
            "提醒患者按医嘱禁食禁饮",
            "核对假牙/首饰取下情况并记录"
        };

        if (order.NeedBloodPrep)
        {
            instructions.Add("告知已安排备血，需配合抽血与交叉配血");
        }

        if (order.HasImplants)
        {
            instructions.Add("再次确认植入物/饰品的处理方案");
        }

        instructions.Add($"麻醉方式：{order.AnesthesiaType}，确认禁食/禁饮时长");

        return new
        {
            instructions,
            allowExceptionNote = true
        };
    }

    private static object BuildNursingPayload(SurgicalOrder order)
    {
        var checklist = new List<string>
        {
            "备皮/皮肤清洁",
            "留置针安置与通路冲洗",
            "采血/标本贴签并扫码",
            "生命体征复测并记录"
        };

        if (order.NeedBloodPrep)
        {
            checklist.Add("交叉配血、血制品温控核对");
        }

        checklist.Add($"准备{order.AnesthesiaType}所需管路与设备");

        return new
        {
            checklist,
            requiresScan = true,
            allowIncidentRecord = true
        };
    }

    private static object BuildSupplyPayload(SurgicalOrder order)
    {
        var items = BuildSupplyItems(order)
            .Select(i => new { i.Name, i.Quantity, scanned = false })
            .ToList();

        return new
        {
            requiredItems = items,
            scanMode = "PerItem",
            autoCompleteWhenAllScanned = items.Count > 0
        };
    }

    private static IReadOnlyList<SupplyItem> BuildSupplyItems(SurgicalOrder order)
    {
        var items = new List<SupplyItem>();

        items.AddRange(ParseRequiredMeds(order));

        items.Add(new SupplyItem($"{order.SurgeryName} 器械包", "1套"));

        if (order.NeedBloodPrep)
        {
            items.Add(new SupplyItem("血制品/备血单", "按需求"));
        }

        if (order.HasImplants)
        {
            items.Add(new SupplyItem("植入物确认表", "1份"));
        }

        return items
            .GroupBy(i => i.Name)
            .Select(g => g.First())
            .ToList();
    }

    private static IReadOnlyList<SupplyItem> ParseRequiredMeds(SurgicalOrder? order)
    {
        if (order == null || string.IsNullOrWhiteSpace(order.RequiredMeds))
        {
            return Array.Empty<SupplyItem>();
        }

        try
        {
            using var doc = JsonDocument.Parse(order.RequiredMeds);
            if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                return items
                    .EnumerateArray()
                    .Select(item => new SupplyItem(
                        item.GetProperty("name").GetString() ?? string.Empty,
                        item.TryGetProperty("qty", out var qtyProp) ? qtyProp.GetString() : null))
                    .ToList();
            }
        }
        catch
        {
            // ignore malformed json, nurses仍可手录
        }

        return Array.Empty<SupplyItem>();
    }

    private sealed record PreOpStepDescriptor(
        string StepCode,
        string StepName,
        TimeSpan PlannedOffset,
        Func<SurgicalOrder, object> PayloadBuilder);

    private sealed record SupplyItem(string Name, string? Quantity);
}
