﻿using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public partial class StructuralLoadCase
  {
    public StructuralLoadCase() { }

    public StructuralLoadCase(StructuralLoadCaseType caseType, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.CaseType = caseType;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralLoadTask
  {
    public StructuralLoadTask() { }

    public StructuralLoadTask(StructuralLoadTaskType taskType, string[] loadCaseRefs, double[] loadFactors, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.TaskType = taskType;
      this.LoadCaseRefs = loadCaseRefs.ToList();
      this.LoadFactors = loadFactors.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralLoadTaskBuckling
  {
    public StructuralLoadTaskBuckling() { }

    public StructuralLoadTaskBuckling(int numModes, int maxNumIterations, string resultCaseRef, string applicationId = null, Dictionary<string, object> properties = null, string stageDefinitionRef = null)
    {
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.NumModes = numModes;
      this.MaxNumIterations = maxNumIterations;
      this.ResultCaseRef = resultCaseRef;
      this.StageDefinitionRef = stageDefinitionRef;
      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralLoadCombo
  {
    public StructuralLoadCombo() { }

    public StructuralLoadCombo(StructuralLoadComboType comboType, string[] loadTaskRefs, double[] loadTaskFactors, string[] loadComboRefs, double[] loadComboFactors, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.ComboType = comboType;
      this.LoadTaskRefs = loadTaskRefs.ToList();
      this.LoadTaskFactors = loadTaskFactors.ToList();
      this.LoadComboRefs = loadComboRefs.ToList();
      this.LoadComboFactors = loadComboFactors.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralGravityLoading
  {
    public StructuralGravityLoading() { }

    public StructuralGravityLoading(StructuralVectorThree gravityFactors, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.GravityFactors = gravityFactors;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }
  public partial class StructuralLoadPlane
  {
    public StructuralLoadPlane() { }

    public StructuralLoadPlane(StructuralAxis loadPlaneAxis, int elementDimension, double tolerance, int span, double spanAngle, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.LoadPlaneAxis = loadPlaneAxis;
      this.ElementDimension = elementDimension;
      this.Tolerance = tolerance;
      this.Span = span;
      this.SpanAngle = spanAngle;
      this.ApplicationId = applicationId;

      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Tolerance *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }
  //public partial class StructuralLoadPlane : StructuralAxis, IStructural
  //{
  //  public override string Type { get => "StructuralLoadPlane"; }

  //  /// <summary>Plane of load.</summary>
  //  [JsonProperty("loadPlane", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
  //  public StructuralAxis LoadPlane { get; set; }

  //  /// <summary>Type elements to apply load to (1D or 2D).</summary>
  //  [JsonProperty("elementDimension", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
  //  public int? ElementDimension { get; set; }

  //  /// <summary>Tolerance for element inclusion.</summary>
  //  [JsonProperty("tolerance", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
  //  public double? Tolerance { get; set; }

  //  /// <summary>Span option.</summary>
  //  [JsonProperty("span", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
  //  public int? Span { get; set; }

  //  /// <summary>Span option.</summary>
  //  [JsonProperty("spanAngle", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
  //  public double? SpanAngle { get; set; }
  //}
  //[Serializable]
  public partial class Structural0DLoad
  {
    public Structural0DLoad() { }

    public Structural0DLoad(StructuralVectorSix loading, string[] nodeRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.NodeRefs = nodeRefs == null ? null : nodeRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }
  public partial class Structural0DLoadPoint
  {
    public Structural0DLoadPoint() { }

    public Structural0DLoadPoint(double[] value, StructuralLoadPlane loadPlane, StructuralVectorThree loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.LoadPlane = loadPlane;
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (var i = 0; i < Value.Count(); i++)
        Value[i] *= factor;
      this.LoadAxis.Origin.Scale(factor);
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DLoad
  {
    public Structural1DLoad() { }

    public Structural1DLoad(StructuralVectorSix loading, string[] elementRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.ElementRefs = elementRefs == null ? null : elementRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DLoadLine
  {
    public Structural1DLoadLine() { }

    public Structural1DLoadLine(double[] value, StructuralVectorSix loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.Value = value.ToList();
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (var i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DLoad
  {
    public Structural2DLoad() { }

    public Structural2DLoad(StructuralVectorThree loading, string[] elementRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.ElementRefs = elementRefs == null ? null : elementRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DLoadPanel
  {
    public Structural2DLoadPanel() { }

    public Structural2DLoadPanel(double[] value, StructuralVectorThree loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.Value = value.ToList();
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (var i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DThermalLoad
  {
    public Structural2DThermalLoad() { }

    public Structural2DThermalLoad(double topTemperature, double bottomTemperature, string loadCaseRef, string[] elementRefs, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.TopTemperature = topTemperature;
      this.BottomTemperature = bottomTemperature;
      this.LoadCaseRef = loadCaseRef;
      this.ElementRefs = elementRefs.ToList();
    }

    public override void Scale(double factor)
    {
      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }
}
