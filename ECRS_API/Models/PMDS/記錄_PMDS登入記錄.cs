using System;
using System.Collections.Generic;

namespace ECRS_API.Models.PMDS;

public partial class 記錄_PMDS登入記錄
{
    public int 主鍵 { get; set; }

    public string? 帳號 { get; set; }

    public DateTime? 建立時間 { get; set; }
}
