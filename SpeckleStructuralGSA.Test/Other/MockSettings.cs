﻿using System;
using System.Collections.Generic;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  public class MockSettings : IGSASettings
  {
    public bool SendOnlyMeaningfulNodes = true;
    public bool SeparateStreams = false;
    public int PollingRate = 2000;

    //Default values for properties specified in the interface
    public string Units { get; set; } = "m";
    public GSATargetLayer TargetLayer { get; set; } = GSATargetLayer.Design;
    public double CoincidentNodeAllowance { get; set; } = 0.1;
    public bool SendOnlyResults { get; set; } = false;

    public bool SendResults { get; set; } = false;

    public Dictionary<string, IGSAResultParams> NodalResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> Element1DResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> Element2DResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> MiscResults { get; set; } = new Dictionary<string, IGSAResultParams>();

    public List<string> ResultCases { get; set; } = new List<string>();
    public bool ResultInLocalAxis { get; set; } = false;
    public int Result1DNumPosition { get; set; } = 3;
    public bool EmbedResults { get; set; } = true;

    public string ObjectUrl(string id)
    {
      return "";
    }
  }
}
