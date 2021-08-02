using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL.1", new string[] { "TASK.2" }, "model", true, true, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) }, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) })]
  public class GSALoadTaskBuckling : GSABase<StructuralLoadTaskBuckling>
  {

    public void ParseGWACommand()
    {
      
    }

    public string SetGWACommand()
    {
      if (this.Value == null  || string.IsNullOrEmpty(this.Value.ApplicationId))
        return "";

      var gwaCommands = new List<string>();

      var loadTask = this.Value as StructuralLoadTaskBuckling;

      var keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      var subkeyword = typeof(GSALoadTaskBuckling).GetSubGSAKeyword().First();

      var taskIndex = Initialiser.AppResources.Cache.ResolveIndex("TASK.2", loadTask.ApplicationId);
      var comboIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSALoadCombo).GetGSAKeyword(), loadTask.ResultCaseRef);
      var stageIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSAConstructionStage).GetGSAKeyword(), loadTask.StageDefinitionRef);

      var ls = new List<string>
        {
          "SET",
          subkeyword,
          taskIndex.ToString(),
          string.IsNullOrEmpty(loadTask.Name) ? " " : loadTask.Name, // Name
          (stageIndex == null) ? "0" : stageIndex.ToString(), // Stage
          "GSS",
          "BUCKLING",
          "1",
          loadTask.NumModes.ToString(),
          loadTask.MaxNumIterations.ToString(),
          (comboIndex == null) ? "0" : "C" + comboIndex,
          "none",
          "none",
          "DRCMEFNSQBHU*",
          "MIN",
          "AUTO",
          "0",
          "0",
          "0","" +
          "NONE",
          "FATAL",
          "NONE",
          "NONE",
          "RAFT_LO",
          "RESID_NO",
          "0",
          "1"
        };
      var command = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);

      gwaCommands.Add(command);
      //Initialiser.AppResources.Proxy.RunGWACommand(command);

      for (var i = 0; i < loadTask.NumModes; i++)
      {
        var caseIndex = Initialiser.AppResources.Cache.ResolveIndex(keyword);
        // Set ANAL
        ls.Clear();
        ls.AddRange(new[] {
          "SET",
          keyword,
          caseIndex.ToString(),
          string.IsNullOrEmpty(loadTask.Name) ? " " : loadTask.Name,
          taskIndex.ToString().ToString(),
          "M" + (i + 1) //desc
        });
        command = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);
        //Initialiser.AppResources.Proxy.RunGWACommand(command);
        gwaCommands.Add(command);
      }

      return string.Join("\n", gwaCommands);
    }
    
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadTaskBuckling loadTask)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(loadTask, () => new GSALoadTaskBuckling() { Value = loadTask }.SetGWACommand());
    }

    // TODO: Same keyword as StructuralLoadTask so will conflict. Need a way to differentiate between.

    public static SpeckleObject ToSpeckle(this GSALoadTaskBuckling dummyObject)
    {
      

      return new SpeckleNull();
    }
  }
}
