using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  //Copied directly from SpeckleGSA
  public class GsaResultParams : IGSAResultParams
  {
    public int ResHeader { get; set; }
    public int Flags { get; set; }
    public List<string> Keys { get; set; } //Field/column headers
    public string Keyword { get; set; }  //TODO: is this still necessary?

    public GsaResultParams(int resHeader, int flags, List<string> keys)
    {
      this.ResHeader = resHeader;
      this.Flags = flags;
      this.Keys = keys;
    }

    public GsaResultParams(string keyword, int resHeader, int flags, List<string> keys)
    {
      this.ResHeader = resHeader;
      this.Flags = flags;
      this.Keys = keys;
      if (!string.IsNullOrEmpty(keyword))
      {
        this.Keyword = keyword;
      }
    }
  }
}
