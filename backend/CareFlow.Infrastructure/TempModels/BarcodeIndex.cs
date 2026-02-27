using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class BarcodeIndex
{
    public string Id { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public string? ImagePath { get; set; }

    public long? ImageSize { get; set; }

    public string? ImageMimeType { get; set; }

    public DateTime? ImageGeneratedAt { get; set; }

    public DateTime CreateTime { get; set; }
}
