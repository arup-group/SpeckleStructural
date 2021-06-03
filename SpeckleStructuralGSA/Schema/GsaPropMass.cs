using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.PROP_MASS, GwaSetCommandType.Set, true)]
  public class GsaPropMass : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public double Mass;
    public double Ixx;
    public double Iyy;
    public double Izz;
    public double Ixy;
    public double Iyz;
    public double Izx;
    //The rest of the parameters are not implemented at this stage

    public GsaPropMass() : base()
    {
      //Defaults
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      FromGwaByFuncs(items, out remainingItems, AddName);

      items = remainingItems.Skip(1).ToList();  //Skip colour

      //PROP_MASS.3 | num | name | colour | mass | Ixx | Iyy | Izz | Ixy | Iyz | Izx | mod { | mod_x | mod_y | mod_z }
      //Zero values are valid for origin, but not for vectors below
      if (!FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out Mass), 
        (v) => double.TryParse(v, out Ixx), (v) => double.TryParse(v, out Iyy), (v) => double.TryParse(v, out Izz),
        (v) => double.TryParse(v, out Ixy), (v) => double.TryParse(v, out Iyz), (v) => double.TryParse(v, out Izx)))
      {
        return false;
      }

      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //PROP_MASS.3 | num | name | colour | mass | Ixx | Iyy | Izz | Ixy | Iyz | Izx | mod { | mod_x | mod_y | mod_z }
      AddItems(ref items, Name, "NO_RGB", Mass, Ixx, Iyy, Izz, Ixy, Iyz, Izx, "MOD", "100%", "100%", "100%");

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }
  }
}
