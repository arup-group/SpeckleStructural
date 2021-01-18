﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_PLANE.4", new string[] { "AXIS.1" }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSAStorey : GSABase<StructuralStorey>
  {
    public bool ParseGWACommand()
    {
      if (this.GWACommand == null)
        return false;

      var pieces = this.GWACommand.ListSplit(Initialiser.Instance.Interface.GwaDelimiter);

      if (pieces[3].ToLower() != "storey")
      {
        return false;
      }

      var obj = new StructuralStorey();

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO 

      this.Value = obj;

      return true;
    }
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSAStorey dummyObject)
    {
      var newLines = ToSpeckleBase<GSAStorey>();
      var typeName = dummyObject.GetType().Name;
      var storeys = new List<GSAStorey>();

      foreach (var k in newLines.Keys)
      {
        var storey = new GSAStorey() { GWACommand = newLines[k] };
        try
        {
          if (storey.ParseGWACommand())
          {
            storeys.Add(storey);
          }
        }
        catch (Exception ex)
        {
          Initialiser.Instance.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }
      }

      Initialiser.Instance.GSASenderObjects.AddRange(storeys);

      return (storeys.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
