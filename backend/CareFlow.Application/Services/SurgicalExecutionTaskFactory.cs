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
        new(
            "PRE_EDU",
            "术前宣教确认",
            TimeSpan.FromHours(-16),
            order => new
            {
                instructions = new[]
                {
                    "提醒患者按医嘱禁食禁饮",
                    "取下假牙、首饰，并录入交接记录",
                    "若患者配合度差需记录异常备注"
                },
                allowExceptionNote = true
            }),
        new(
            "PRE_NURSING",
            "护理操作和管路准备",
            TimeSpan.FromHours(-12),
            order => new
            {
                checklist = new[]
                {
                    "备皮/皮肤清洁",
                    "留置针安置与通路冲洗",
                    "采血/标本贴签并扫码",
                    "生命体征复测与不良事件上报"
                },
                requiresScan = true
            }),
        new(
            "PRE_SUPPLY",
            "手术带入物品核对",
            TimeSpan.FromHours(-6),
            order => new
            {
                requiredItems = ParseRequiredMeds(order as SurgicalOrder),
                scanMode = "PerItem",
                autoCompleteWhenAllScanned = true
            })
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
                Id = surgicalOrder.Id,
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

    private static IReadOnlyList<object> ParseRequiredMeds(SurgicalOrder? surgicalOrder)
    {
        if (surgicalOrder == null || string.IsNullOrWhiteSpace(surgicalOrder.RequiredMeds))
        {
            return Array.Empty<object>();
        }

        try
        {
            using var doc = JsonDocument.Parse(surgicalOrder.RequiredMeds);
            if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                return items
                    .EnumerateArray()
                    .Select(item => new
                    {
                        name = item.GetProperty("name").GetString() ?? string.Empty,
                        qty = item.TryGetProperty("qty", out var qtyProp) ? qtyProp.GetString() : null,
                        scanned = false
                    })
                    .Cast<object>()
                    .ToList();
            }
        }
        catch
        {
            // ignore malformed json, nurses仍可手录
        }

        return Array.Empty<object>();
    }

    private sealed record PreOpStepDescriptor(
        string StepCode,
        string StepName,
        TimeSpan PlannedOffset,
        Func<SurgicalOrder, object> PayloadBuilder);
}
