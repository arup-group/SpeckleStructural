using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using System.Threading.Tasks;

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

      int numToBeSent = 0;

      //foreach (var i in newNodeLines.Keys)
      Parallel.ForEach(newNodeLines.Keys, i =>
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
            };

            if (gsaNode.MeshSize.HasValue && gsaNode.MeshSize.Value > 0)
            {
              structuralNode.GSALocalMeshSize = gsaNode.MeshSize.Value;
            }

            if (gsaNode.NodeRestraint == NodeRestraint.Custom && gsaNode.Restraints != null && gsaNode.Restraints.Count() > 0)
            {
              structuralNode.Restraint = Helper.AxisDirDictToStructuralVectorBoolSix(gsaNode.Restraints);
            }
            else if (gsaNode.NodeRestraint == NodeRestraint.Fix)
            {
              structuralNode.Restraint = new StructuralVectorBoolSix(Enumerable.Repeat(true, 6));
            }
            else if (gsaNode.NodeRestraint == NodeRestraint.Pin)
            {
              structuralNode.Restraint = new StructuralVectorBoolSix(Enumerable.Repeat(true, 3).Concat(Enumerable.Repeat(false, 3)));
            }

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
          Initialiser.GsaKit.GSASenderObjects.Add(new GSANode() { Value = (StructuralNode)objNode, GSAId = i });
          numToBeSent++;
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
            Initialiser.GsaKit.GSASenderObjects.Add(new GSA0DSpring() { Value = (Structural0DSpring)objSpring, GSAId = i });
            numToBeSent++;
          }
        }
      }
      );

      return (numToBeSent > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
