using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class 稽查項目上傳檔案
{
    public int PKey { get; set; }

    public string EventId { get; set; } = null!;

    public string? FileName { get; set; }

    public string? Extension { get; set; }

    public string? ZipFileName { get; set; }

    public string? CreateUser { get; set; }

    public DateTime? CreateDate { get; set; }

    public bool? IsDelete { get; set; }

    public DateTime? DeleteDate { get; set; }
}
