using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class vw_許可證對應表
{
    public string 原許可證代碼 { get; set; } = null!;

    public string? PMDS許可證代碼 { get; set; }
}
