﻿using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.LOAD_TITLE, GwaSetCommandType.Set, StreamBucket.Model)]
  public class GsaLoadCase : GsaRecord
  {
    public StructuralLoadCaseType CaseType;
    public string Title;

    public GsaLoadCase() : base()
    {
      //Defaults
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //Only basic level of support is offered now - the arguments after type are ignored
      //LOAD_TITLE.2 | case | title | type | source | category | dir | include | bridge
      //Note: case is deserialised into the Index field
      FromGwaByFuncs(items, out remainingItems, AddTitle, AddType);
      
      return true;
    }

    public bool AddTitle(string v)
    {
      Title = string.IsNullOrEmpty(v) ? null : v;
      return true;
    }

    public bool AddType(string v)
    {
      CaseType = SchemaConversion.Helper.StringToLoadCaseType(v);
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_TITLE.2 | case | title | type | source | category | dir | include | bridge
      //Note: case will be serialised from the Index field
      AddItems(ref items, Title, SchemaConversion.Helper.LoadCaseTypeToString(CaseType), 1, "~", "NONE", "INC_BOTH");

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }


  }
}
