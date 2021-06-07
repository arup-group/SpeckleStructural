using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaNodeToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaNode dummyObject)
    {
      var nodeKw = GsaRecord.GetKeyword<GsaNode>();
      var springKw = GsaRecord.GetKeyword<GsaPropSpr>();
      var massKw = GsaRecord.GetKeyword<GsaPropMass>();

      var newNodeLines = Initialiser.AppResources.Cache.GetGwaToSerialise(nodeKw);

      //This method produces two types of SpeckleStructural objects
      var structuralNodes = new List<StructuralNode>();
      var structural0dSprings = new List<Structural0DSpring>();

      foreach (var i in newNodeLines.Keys)
      {
        GsaNode gsaNode = null;
        var objNode = Helper.ToSpeckleTryCatch(nodeKw, i, () =>
        {
          gsaNode = new GsaNode();
          if (gsaNode.FromGwa(newNodeLines[i]))
          {
            var structuralNode = new StructuralNode()
            {
              Name = gsaNode.Name,
              ApplicationId = SpeckleStructuralGSA.Helper.GetApplicationId(nodeKw, i),
              Value = new List<double>() { gsaNode.X, gsaNode.Y, gsaNode.Z },
              GSALocalMeshSize = gsaNode.MeshSize ?? 0,
              Restraint = Helper.AxisDirDictToStructuralVectorBoolSix(gsaNode.Restraints)
            };

            if (gsaNode.MassPropertyIndex.HasValue && gsaNode.MassPropertyIndex.Value > 0)
            {
              var massGwas = Initialiser.AppResources.Cache.GetGwa(massKw, gsaNode.MassPropertyIndex.Value);
              if (massGwas != null && massGwas.Count() > 0 && !string.IsNullOrEmpty(massGwas.First()))
              {
                var gsaPropMass = new GsaPropMass();
                if (gsaPropMass.FromGwa(massGwas.First()) && gsaPropMass.Mass > 0)
                {
                  structuralNode.Mass = gsaPropMass.Mass;
                }
              }
            }

            return structuralNode;
          }
          return new SpeckleNull();
        });

        if (!(objNode is SpeckleNull))
        {
          structuralNodes.Add((StructuralNode)objNode);
        }

        if (gsaNode.SpringPropertyIndex.HasValue && gsaNode.SpringPropertyIndex.Value > 0)
        {
          var objSpring = Helper.ToSpeckleTryCatch(nodeKw, i, () =>
          {
            var springPropRef = SpeckleStructuralGSA.Helper.GetApplicationId(springKw, gsaNode.SpringPropertyIndex.Value);
            if (!string.IsNullOrEmpty(springPropRef))
            {
              var structural0dSpring = new Structural0DSpring()
              {
                //The application ID might need a better mechanism to allow a StructuralNode and Structural0DSpring previously received
                //that originally created the one node to be separated out again to produce the Application ID to use here for this spring
                //TO DO - review, for now just append a string to ensure the same Application ID value isn't used twice
                ApplicationId = gsaNode.ApplicationId + "_spring",
                Name = gsaNode.Name,
                Value = new List<double>() { gsaNode.X, gsaNode.Y, gsaNode.Z },
                PropertyRef = springPropRef,
                Dummy = false
              };
              return structural0dSpring;
            }
            return new SpeckleNull();
          });
          if (!(objSpring is SpeckleNull))
          {
            structural0dSprings.Add((Structural0DSpring)objSpring);
          }
        }
      }

      var nodes = structuralNodes.Select(n => new GSANode() { Value = n }).ToList();
      var springs = structural0dSprings.Select(s => new GSA0DSpring() { Value = s }).ToList();

      if (nodes.Count() > 0)
      {
        Initialiser.GsaKit.GSASenderObjects.AddRange(nodes);
      }
      if (springs.Count() > 0)
      {
        Initialiser.GsaKit.GSASenderObjects.AddRange(springs);
      }
      return (nodes.Count() > 0 || springs.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
