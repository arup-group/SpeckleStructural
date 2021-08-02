using System;
using System.Collections.Generic;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  //TO DO: remove this class as it doesn't seem to materially offer functionality used only in tseting
  public class MockSettings : IGSASettings
  {
    public bool SendOnlyMeaningfulNodes = true;
    public bool SeparateStreams = false;
    public int PollingRate = 2000;

    //Default values for properties specified in the interface
    public string Units { get; set; } = "m";
    public GSATargetLayer TargetLayer { get; set; } = GSATargetLayer.Design;
    public double CoincidentNodeAllowance { get; set; } = 0.1;
    public StreamContentConfig StreamSendConfig { get; set; }

    /*
    public Dictionary<string, IGSAResultParams> NodalResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> Element1DResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> Element2DResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    public Dictionary<string, IGSAResultParams> MiscResults { get; set; } = new Dictionary<string, IGSAResultParams>();
    */

    public List<string> ResultCases { get; set; } = new List<string>();
    public bool ResultInLocalAxis { get; set; } = false;
    public int Result1DNumPosition { get; set; } = 3;
    public List<ResultType> ResultTypes { get; set; } = new List<ResultType>();

    public string ObjectUrl(string id)
    {
      return "";
    }
  }
}
