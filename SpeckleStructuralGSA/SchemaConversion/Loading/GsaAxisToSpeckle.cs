﻿using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleCoreGeometryClasses;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaAxisToSpeckle
  {
    //NOTE: *unlike* most other ToSpeckle methods, this one does not find, and then convert, all instances of a GSA entity type; 
    //this method does convert the exact object passed into a corresponding Speckle object
    public static SpeckleObject ToSpeckle(this GsaAxis gsaAxis)
    {
      var vX = new Vector3D(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value);
      var vXY = new Vector3D(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value);
      var normal = vX.CrossProduct(vXY);
      var vY = normal.CrossProduct(vX);

      var axisX = new StructuralVectorThree(vX.X, vX.Y, vX.Z);
      var axisY = new StructuralVectorThree(vY.X, vY.Y, vY.Z);
      var axisNormal = new StructuralVectorThree(normal.X, normal.Y, normal.Z);
      //The x axis is assumed to be the , but the XY vector is not necessarily 
      var speckleAxis = new StructuralAxis(axisX, axisY, axisNormal, gsaAxis.ApplicationId)
      {
        Origin = new SpecklePoint(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ),
        Name = gsaAxis.Name
      };

      return speckleAxis;
    }
  }
}
