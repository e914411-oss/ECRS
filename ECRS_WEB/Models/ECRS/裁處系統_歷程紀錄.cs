using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class 裁處系統_歷程紀錄
{
    public int 裁處歷程主鍵 { get; set; }

    public int 裁處案件編號 { get; set; }

    public DateTime 日期 { get; set; }

    public string 異動人員 { get; set; } = null!;

    public string 異動目的 { get; set; } = null!;
}
