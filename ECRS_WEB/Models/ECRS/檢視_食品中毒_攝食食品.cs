using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class 檢視_食品中毒_攝食食品
{
    public int 食品中毒速報主表主鍵 { get; set; }

    public string? 攝食食品 { get; set; }

    public string 攝食食品名稱 { get; set; } = null!;
}
