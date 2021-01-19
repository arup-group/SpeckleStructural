﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    private static GsaKit gsaKit = new GsaKit();

    public static IGSAKit GsaKit { get => gsaKit; }
    public static IGSAAppResources AppResources { get; set; }

    /*
    public IGSASettings Settings { get; set; }
    public IGSAProxy Interface { get; set; }
    public IGSACacheForKit Cache { get; set; }
    public ISpeckleGSAAppUI AppUI { get; set; }
    */
  }

  public class GsaKit : IGSAKit
  {
    private readonly List<TypeDependencyData> typeDepData = new List<TypeDependencyData>();

    //The variable below must be a property (i.e. with { get; }) and of Dictionary<Type, List<object>> type so that SpeckleGSA
    //can recognise this as a kit it can work with
    public IGSASenderDictionary GSASenderObjects { get; } = new GSASenderDictionary();

    //This is a dictionary of types (and their keywords for advance GSA record index reservation) which can be processed in parallel 
    //ensure the order they appear in GSA matches the order in the stream.
    //In most cases a type is added to this list if:
    //- in practice objects of the type are one of the most numerous in a typical stream
    //- indices can be managed as a COM client - for doesn't work for nodes since the NodeAt does it
    //- there is a simple 1:1 relationship between objects of that type and GSA records, enabling GSA record indices to be reserved first, 
    //  then processed in parallel
    public Dictionary<Type, string> RxParallelisableTypes
    {
      get
      {
        var elementKw = Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design ? GwaKeyword.MEMB : GwaKeyword.EL;
        return new Dictionary<Type, string>()
        { { typeof(StructuralLoadCase), GwaKeyword.LOAD_TITLE.GetStringValue() },
          { typeof(StructuralLoadCombo), GwaKeyword.COMBINATION.GetStringValue() },
          { typeof(StructuralAssembly), GwaKeyword.ASSEMBLY.GetStringValue() },
          { typeof(Structural1DElement), elementKw.GetStringValue() },
          { typeof(Structural2DElement), elementKw.GetStringValue() },
          { typeof(Structural2DElementMesh), elementKw.GetStringValue() },
          { typeof(Structural2DVoid), elementKw.GetStringValue() },
          { typeof(Structural1DProperty), GwaKeyword.PROP_SEC.GetStringValue() },
          { typeof(Structural1DPropertyExplicit), GwaKeyword.SECTION.GetStringValue() },
          { typeof(Structural2DProperty), GwaKeyword.PROP_2D.GetStringValue() },
          { typeof(StructuralSpringProperty), GwaKeyword.PROP_SPR.GetStringValue() },
          { typeof(StructuralMaterialConcrete), GwaKeyword.MAT_CONCRETE.GetStringValue() },
          { typeof(StructuralMaterialSteel), GwaKeyword.MAT_STEEL.GetStringValue() }
        };
      }
    }

    //For now, it returns old GSA schema (i.e. those implementing the IGSASpeckleContainer and IGSAContainer interfaces.
    //When the new GSA schema is fully integrated, it will return SpeckleStructural types
    public Dictionary<Type, List<Type>> RxTypeDependencies
    {
      get
      {
        if (!typeDepData.Any(td => td.Direction == StreamDirection.Receive && td.Layer == Initialiser.AppResources.Settings.TargetLayer))
        {
          var typeDeps = TypeDependencies(StreamDirection.Receive);
          typeDepData.Add(new TypeDependencyData(StreamDirection.Receive, Initialiser.AppResources.Settings.TargetLayer, typeDeps));
        }
        return typeDepData.FirstOrDefault(td => td.Direction == StreamDirection.Receive && td.Layer == Initialiser.AppResources.Settings.TargetLayer).Dependencies;
      }
    }

    public Dictionary<Type, List<Type>> TxTypeDependencies
    {
      get
      {
        if (!typeDepData.Any(td => td.Direction == StreamDirection.Send && td.Layer == Initialiser.AppResources.Settings.TargetLayer))
        {
          var typeDeps = TypeDependencies(StreamDirection.Send);
          typeDepData.Add(new TypeDependencyData(StreamDirection.Send, Initialiser.AppResources.Settings.TargetLayer, typeDeps));
        }
        return typeDepData.FirstOrDefault(td => td.Direction == StreamDirection.Send && td.Layer == Initialiser.AppResources.Settings.TargetLayer).Dependencies;
      }
    }

    public List<string> Keywords => layerKeywords[Initialiser.AppResources.Settings.TargetLayer].Select(kw => kw.GetStringValue()).ToList();

    //This dictionary should be independent of layer
    private readonly Dictionary<GSATargetLayer, Dictionary<Type, GwaKeyword>> layerKeywordTypes;

    private readonly List<Type> schemaTypes = new List<Type>();  //ALL schema types, from both layers
    private readonly List<Type> oldSchemaTypes = new List<Type>();  //ALL old schema types, from both layers

    //The reason for both layerKeywords and layerKeywordTypes existing is because not all keywords have types
    private readonly Dictionary<GSATargetLayer, List<GwaKeyword>> layerKeywords;

    public GsaKit()
    {
      var assembly = GetType().Assembly; //This assembly
      var assemblyTypes = assembly.GetTypes();

      var gsaBaseType = typeof(GsaRecord);
      var gsaAttributeType = typeof(GsaType);
      schemaTypes.AddRange(assemblyTypes.Where(t => t.IsSubclassOf(gsaBaseType) && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)));

      var oldGsaInterface = typeof(IGSASpeckleContainer);
      oldSchemaTypes.AddRange(assemblyTypes.Where(t => oldGsaInterface.IsAssignableFrom(t) && !t.IsAbstract));

      //First get all keywords associated with implemented classes of the layer - this is needed because the referenced keywords 
      //in the GsaType attribute of GsaRecord classes sometimes span both layers
      layerKeywordTypes = new Dictionary<GSATargetLayer, Dictionary<Type, GwaKeyword>>
      {
        { GSATargetLayer.Design, new Dictionary<Type, GwaKeyword>() },
        { GSATargetLayer.Analysis, new Dictionary<Type, GwaKeyword>() }
      };

      //These aren't limited to whether these keywords are implemented in any classes as yet
      layerKeywords = new Dictionary<GSATargetLayer, List<GwaKeyword>>
      {
        { GSATargetLayer.Design, new List<GwaKeyword>() },
        { GSATargetLayer.Analysis, new List<GwaKeyword>() }
      };

      var relevantSchemaTypes = schemaTypes.Where(t => GsaRecord.IsSelfContained(t));

      GenerateLayerKeywords(relevantSchemaTypes);
      GenerateLayerKeywordTypes(relevantSchemaTypes);
    }

    public void Clear()
    {
      throw new NotImplementedException();
    }

    #region private_methods

    private void GenerateLayerKeywordTypes(IEnumerable<Type> relevantSchemaTypes)
    {
      foreach (var t in relevantSchemaTypes)
      {
        foreach (var layer in new[] { GSATargetLayer.Design, GSATargetLayer.Analysis })
        {
          if (MatchesLayer(t, layer))
          {
            layerKeywordTypes[layer].Add(t, GsaRecord.GetGwaKeyword(t));
          }
        }
      }
    }

    private void GenerateLayerKeywords(IEnumerable<Type> relevantSchemaTypes)
    {
      //This dictionary should be independent of layer
      var keywordDependencies = relevantSchemaTypes.ToDictionary(t => GsaRecord.GetGwaKeyword(t), t => GsaRecord.GetReferencedKeywords(t));

      var implementedKwsByLayer = relevantSchemaTypes.ToDictionary(t => GsaRecord.GetGwaKeyword(t), t => TypeLayers(t));
      foreach (var kw in keywordDependencies.Keys)
      {
        foreach (var layer in new[] { GSATargetLayer.Design, GSATargetLayer.Analysis })
        {
          if (implementedKwsByLayer[kw][layer])
          {
            if (!layerKeywords[layer].Contains(kw))
            {
              layerKeywords[layer].Add(kw);
            }
          }
          if (keywordDependencies[kw] != null && keywordDependencies[kw].Length > 0)
          {
            foreach (var kwPrereq in keywordDependencies[kw])
            {
              //The attributes on the types are the container for knowing which layer a keyword is for.  For each referenced
              //(i.e. prerequiste) keyword of a keyword, a corresponding type will need to be found in order to determine which
              //layer that referenced keyword is on.
              //However, at any point in time, there might not be a type created yet for that referenced keyword.  For these cases,
              //assume that the referenced keyword is for both layers.

              if (((implementedKwsByLayer.ContainsKey(kwPrereq) && implementedKwsByLayer[kwPrereq][layer]) || !implementedKwsByLayer.ContainsKey(kwPrereq))
                && (!layerKeywords[layer].Contains(kwPrereq)))
              {
                layerKeywords[layer].Add(kwPrereq);
              }
            }
          }
        }
      }
    }

    private Dictionary<Type, List<Type>> TypeDependencies(StreamDirection direction)
    {
      var typeDependencies = new Dictionary<Type, List<Type>>();

      //Build up dictionary of new GSA schema types and keywords - to be used to construct dependencies based on these new types
      var layerSchemaDict = layerKeywordTypes[Initialiser.AppResources.Settings.TargetLayer];
      var layerSchemaTypes = layerSchemaDict.Keys;
      var layerSchemaKeywords = layerSchemaDict.Values;
      var kwDependencies = layerSchemaTypes.ToDictionary(t => layerSchemaDict[t],
        t => GsaRecord.GetReferencedKeywords(t).Where(kw => layerSchemaKeywords.Contains(kw)).ToList());

      foreach (var oldT in oldSchemaTypes)
      {
        var oldTKeyword = ((string)oldT.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();
        if (!kwDependencies.Keys.Any(k => k.GetStringValue().Equals(oldTKeyword, StringComparison.InvariantCultureIgnoreCase)))
        {
          continue;
        }

        if (!typeDependencies.ContainsKey(oldT))
        {
          typeDependencies.Add(oldT, new List<Type>());
        }

        var attVal = oldT.GetAttribute<GSAObject>(((direction == StreamDirection.Receive) ? "Write" : "Read") + "Prerequisite");
        var prereqs = (attVal != null) ? ((Type[])attVal).ToList() : new List<Type>();

        foreach (var tPrereq in prereqs)
        {
          //Remove version for comparison with keyword enum
          var kwPrereq = ((string)tPrereq.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();

          if (kwDependencies.Keys.Any(k => k.GetStringValue().Equals(kwPrereq, StringComparison.InvariantCultureIgnoreCase)))
          {
            typeDependencies[oldT].Add(tPrereq);
          }
        }
      }

      return typeDependencies;
    }

    private Dictionary<GSATargetLayer, bool> TypeLayers(Type t)
    {
      return new Dictionary<GSATargetLayer, bool>
      {
        { GSATargetLayer.Design, GsaRecord.IsDesignLayer(t) },
        { GSATargetLayer.Analysis, GsaRecord.IsAnalysisLayer(t) }
      };
    }

    private bool MatchesLayer(Type t, GSATargetLayer layer)
    {
      return ((layer == GSATargetLayer.Design && GsaRecord.IsDesignLayer(t))
            || (layer == GSATargetLayer.Analysis && GsaRecord.IsAnalysisLayer(t)));
    }

    #endregion
  }

  

  internal class TypeDependencyData
  {
    public StreamDirection Direction;
    public GSATargetLayer Layer;
    public Dictionary<Type, List<Type>> Dependencies;

    internal TypeDependencyData(StreamDirection dir, GSATargetLayer layer, Dictionary<Type, List<Type>> deps)
    {
      this.Direction = dir;
      this.Layer = layer;
      this.Dependencies = deps;
    }

    internal TypeDependencyData(StreamDirection dir, GSATargetLayer layer)
    {
      this.Direction = dir;
      this.Layer = layer;
      this.Dependencies = new Dictionary<Type, List<Type>>();
    }

    internal void Add(Type t, List<Type> prereqs)
    {
      if (!Dependencies.ContainsKey(t))
      {
        Dependencies.Add(t, new List<Type>());
      }
      foreach (var pr in prereqs)
      {
        if (!Dependencies[t].Contains(pr))
        {
          Dependencies[t].Add(pr);
        }
      }
    }
  }

  internal enum LayerCoverage
  {
    None,
    Design,
    Analysis,
    Both
  }
}
