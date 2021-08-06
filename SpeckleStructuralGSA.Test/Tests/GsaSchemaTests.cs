﻿using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Moq;
using NUnit.Framework;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;
using SpeckleCoreGeometryClasses;
using DeepEqual.Syntax;
using SpeckleCore;
using SpeckleStructuralGSA.Schema.Bridge;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class GsaSchemaTests
  {
    //Used in multiple tests
    private static readonly GsaAxis gsaAxis1 = new GsaAxis() { Index = 1, ApplicationId = "Axis1", Name = "StandardAxis", XDirX = 1, XDirY = 0, XDirZ = 0, XYDirX = 0, XYDirY = 1, XYDirZ = 0, OriginX = 10, OriginY = 20, OriginZ = 30 };
    private static readonly GsaAxis gsaAxis2 = new GsaAxis() { Index = 2, ApplicationId = "Axis2", Name = "AngledAxis", XDirX = 1, XDirY = 1, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
    private static readonly string streamId1 = "TestStream1";

    [SetUp]
    public void SetUp()
    {
      Initialiser.AppResources = new MockGSAApp(proxy: new TestProxy());
      Initialiser.GsaKit.Clear();
    }

    #region tests
    //tests have been arranged into groups and then alphabetically within each group
    #region simple
    [Test]
    public void GsaAlignSimple()
    {
      var alignGwas = new List<string>()
      {
        "ALIGN.1\t1\tedge\t1\t4\t0\t0.02\t25\t0.02\t50\t-0.01\t150\t-0.01"
      };

      var aligns = new List<GsaAlign>();
      foreach (var g in alignGwas)
      {
        var l = new GsaAlign();
        Assert.IsTrue(l.FromGwa(g));
        aligns.Add(l);
      }
      Assert.AreEqual(1, aligns[0].GridSurfaceIndex);
      Assert.AreEqual(4, aligns[0].NumAlignmentPoints);
      Assert.AreEqual(new List<double>() { 0, 25, 50, 150 }, aligns[0].Chain);
      Assert.AreEqual(new List<double>() { 0.02, 0.02, -0.01, -0.01 }, aligns[0].Curv);

      for (int i = 0; i < aligns.Count(); i++)
      {
        Assert.IsTrue(aligns[i].Gwa(out var gwa));
        Assert.IsTrue(alignGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaAnal()
    {
      var analGwas = new List<string>()
      {
        "ANAL.1\t1\tAnalysis Case 1\t1\tL1"
      };
      var anals = new List<GsaAnal>();
      foreach (var g in analGwas)
      {
        var l = new GsaAnal();
        Assert.IsTrue(l.FromGwa(g));
        anals.Add(l);
      }
      Assert.AreEqual("Analysis Case 1", anals[0].Name);
      Assert.AreEqual(1, anals[0].LoadCase);
      Assert.AreEqual("L1", anals[0].Desc);

      for (int i = 0; i < anals.Count(); i++)
      {
        Assert.IsTrue(anals[i].Gwa(out var gwa));
        Assert.IsTrue(analGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaAnalStage()
    {
      var analStageGwas = new List<string>()
      {
        "ANAL_STAGE.3\t1\tname\tNO_RGB\tall\t0\t0\tall"
      };
      var analStages = new List<GsaAnalStage>();
      foreach (var g in analStageGwas)
      {
        var l = new GsaAnalStage();
        Assert.IsTrue(l.FromGwa(g));
        analStages.Add(l);
      }
      Assert.AreEqual(Colour.NO_RGB, analStages[0].Colour);
      Assert.AreEqual(new List<int>(), analStages[0].List);
      Assert.AreEqual(0, analStages[0].Phi);
      Assert.AreEqual(0, analStages[0].Days);
      Assert.AreEqual(new List<int>(), analStages[0].Lock);

      for (int i = 0; i < analStages.Count(); i++)
      {
        Assert.IsTrue(analStages[i].Gwa(out var gwa));
        Assert.IsTrue(analStageGwas[i].Equals(gwa.First()));
      }
    }

    //This just tests transitions from the GSA schema to GWA commands, and back again, since there is no need at the moment for a ToNativeTest() method for StructuralAxis
    [Test]
    public void GsaAxisSimple()
    {
      Assert.IsTrue(gsaAxis1.Gwa(out var axis1gwa));
      Assert.IsTrue(gsaAxis2.Gwa(out var axis2gwa));

      Assert.IsTrue(ModelValidation(new string[] { axis1gwa.First(), axis2gwa.First() }, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaAxis>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      Assert.IsTrue(gsaAxis1.FromGwa(axis1gwa.First()));
      Assert.IsTrue(gsaAxis2.FromGwa(axis2gwa.First()));
    }

    [Test]
    public void GsaCombination()
    {
      var combinationGwas = new List<string>()
      {
        "COMBINATION.1\t1\tCombination case 1\tA1",
        "COMBINATION.1\t1\tCombination case 2\t1A1 + 1A2 + 1A3\t\tnotes"
      };
      var combinations = new List<GsaCombination>();
      foreach (var g in combinationGwas)
      {
        var l = new GsaCombination();
        Assert.IsTrue(l.FromGwa(g));
        combinations.Add(l);
      }
      Assert.AreEqual("Combination case 1", combinations[0].Name);
      Assert.AreEqual("A1", combinations[0].Desc);
      Assert.IsNull(combinations[0].Bridge);
      Assert.IsNull(combinations[0].Note);

      Assert.AreEqual("Combination case 2", combinations[1].Name);
      Assert.AreEqual("1A1 + 1A2 + 1A3", combinations[1].Desc);
      Assert.IsFalse(combinations[1].Bridge);
      Assert.AreEqual("notes", combinations[1].Note);

      for (int i = 0; i < combinations.Count(); i++)
      {
        Assert.IsTrue(combinations[i].Gwa(out var gwa));
        Assert.IsTrue(combinationGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaElSimple()
    {
      var gsaEls = GenerateMixedGsaEls();
      var gwaToTest = new List<string>();
      foreach (var gsaEl in gsaEls)
      {
        Assert.IsTrue(gsaEl.Gwa(out var gwa, false));

        var gsaElNew = new GsaEl();
        Assert.IsTrue(gsaElNew.FromGwa(gwa.First()));
        gsaEl.ShouldDeepEqual(gsaElNew);

        gwaToTest = gwaToTest.Union(gwa).ToList();
      }

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaEl>(), 2, out var mismatch));
    }

    [Test]
    public void GsaGenRestSimple()
    {
      var genRestGwas = new List<string>()
      {
        "GEN_REST.2\t\t1\t1\t1\t0\t0\t0\t1 2 3\t1"
      };

      var genRests = new List<GsaGenRest>();
      foreach (var g in genRestGwas)
      {
        var l = new GsaGenRest();
        Assert.IsTrue(l.FromGwa(g));
        genRests.Add(l);
      }

      Assert.AreEqual(RestraintCondition.Constrained, genRests[0].X);
      Assert.AreEqual(RestraintCondition.Constrained, genRests[0].Y);
      Assert.AreEqual(RestraintCondition.Constrained, genRests[0].Z);
      Assert.AreEqual(RestraintCondition.Free, genRests[0].XX);
      Assert.AreEqual(RestraintCondition.Free, genRests[0].YY);
      Assert.AreEqual(RestraintCondition.Free, genRests[0].ZZ);
      Assert.Warn("Simple test can not handle node list");
      genRests[0].Node = new List<int>() { 1, 2, 3 };
      Assert.AreEqual(new List<int>() { 1 }, genRests[0].Stage);

      for (int i = 0; i < genRests.Count(); i++)
      {
        Assert.IsTrue(genRests[i].Gwa(out var gwa));
        Assert.IsTrue(genRestGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaGridLineSimple()
    {
      var gridLineGwas = new List<string>()
      {
        "GRID_LINE.1	1	Level	LINE	10	0	50	0	0",
        "GRID_LINE.1	2	Angled	LINE	10	0	50	30	0",
        "GRID_LINE.1	3	Arc	ARC	10	0	50	30	60"
      };

      var gridLines = new List<GsaGridLine>();
      foreach (var g in gridLineGwas)
      {
        var l = new GsaGridLine();
        Assert.IsTrue(l.FromGwa(g));
        Assert.AreEqual(10, l.XCoordinate);
        Assert.AreEqual(0, l.YCoordinate);
        Assert.AreEqual(50, l.Length); // Length (LINE) or radius (ARC)
        gridLines.Add(l);
      }

      Assert.AreEqual(GridLineType.Line, gridLines[0].Type);
      Assert.AreEqual(0, gridLines[0].Theta1);
      Assert.AreEqual(0, gridLines[0].Theta2);

      Assert.AreEqual(GridLineType.Line, gridLines[1].Type);
      Assert.AreEqual(30, gridLines[1].Theta1);
      Assert.AreEqual(0, gridLines[1].Theta2);

      Assert.AreEqual(GridLineType.Arc, gridLines[2].Type);
      Assert.AreEqual(30, gridLines[2].Theta1);
      Assert.AreEqual(60, gridLines[2].Theta2);

      for (int i = 0; i < gridLines.Count(); i++)
      {
        Assert.IsTrue(gridLines[i].Gwa(out var gwa));
        Assert.IsTrue(gridLineGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaInfBeamSimple()
    {
      var infBeamGwas = new List<string>()
      {
        "INF_BEAM.2\tAbutment A Headstock - Positive Bending\t1\t5316\t50%\t1\tFORCE\tYY"
      };
      var infBeams = new List<GsaInfBeam>();
      foreach (var g in infBeamGwas)
      {
        var l = new GsaInfBeam();
        Assert.IsTrue(l.FromGwa(g));
        infBeams.Add(l);
      }
      Assert.AreEqual(1, infBeams[0].Action);
      Assert.AreEqual(5316, infBeams[0].Element);
      Assert.AreEqual(0.5, infBeams[0].Position);
      Assert.AreEqual(1, infBeams[0].Factor);
      Assert.AreEqual(InfType.FORCE, infBeams[0].Type);
      Assert.AreEqual(AxisDirection6.YY, infBeams[0].Direction);

      for (int i = 0; i < infBeams.Count(); i++)
      {
        Assert.IsTrue(infBeams[i].Gwa(out var gwa));
        Assert.IsTrue(infBeamGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaInfNodeSimple()
    {
      var infNodeGwas = new List<string>()
      {
        "INF_NODE.1\tname\t1\t1\t1\tDISP\tGLOBAL\tZ"
      };
      var infNodes = new List<GsaInfNode>();
      foreach (var g in infNodeGwas)
      {
        var l = new GsaInfNode();
        Assert.IsTrue(l.FromGwa(g));
        infNodes.Add(l);
      }
      Assert.AreEqual(1, infNodes[0].Action);
      Assert.AreEqual(1, infNodes[0].Node);
      Assert.AreEqual(1, infNodes[0].Factor);
      Assert.AreEqual(InfType.DISP, infNodes[0].Type);
      Assert.AreEqual(AxisRefType.Global, infNodes[0].AxisRefType);
      Assert.AreEqual(AxisDirection6.Z, infNodes[0].Direction);

      for (int i = 0; i < infNodes.Count(); i++)
      {
        Assert.IsTrue(infNodes[i].Gwa(out var gwa));
        Assert.IsTrue(infNodeGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaLoad2dFaceSimple()
    {
      var load2dFaceGwas = new List<string>()
      {
        //list of entities set to all for simple test. Lists are tested elsewhere
        "LOAD_2D_FACE.2\t\tall\t30\tGLOBAL\tCONS\tNO\tZ\t-2000",
        "LOAD_2D_FACE.2\t\tall\t2\tLOCAL\tGEN\tYES\tZ\t-2000",
        "LOAD_2D_FACE.2\t\tall\t27\tLOCAL\tPOINT\tNO\tZ\t-10000\t0\t0"
      };

      var load2dFaces = new List<GsaLoad2dFace>();
      foreach (var g in load2dFaceGwas)
      {
        var l = new GsaLoad2dFace();
        Assert.IsTrue(l.FromGwa(g));
        load2dFaces.Add(l);
      }
      Assert.AreEqual(30, load2dFaces[0].LoadCaseIndex);
      Assert.AreEqual(AxisRefType.Global, load2dFaces[0].AxisRefType);
      Assert.AreEqual(Load2dFaceType.Uniform, load2dFaces[0].Type);
      Assert.IsFalse(load2dFaces[0].Projected);
      Assert.AreEqual(AxisDirection3.Z, load2dFaces[0].LoadDirection);
      Assert.AreEqual(new List<double> { -2000 }, load2dFaces[0].Values);

      Assert.AreEqual(2, load2dFaces[1].LoadCaseIndex);
      Assert.AreEqual(AxisRefType.Local, load2dFaces[1].AxisRefType);
      Assert.AreEqual(Load2dFaceType.General, load2dFaces[1].Type);
      Assert.IsTrue(load2dFaces[1].Projected);
      Assert.AreEqual(AxisDirection3.Z, load2dFaces[1].LoadDirection);
      Assert.AreEqual(new List<double> { -2000 }, load2dFaces[1].Values);

      Assert.AreEqual(27, load2dFaces[2].LoadCaseIndex);
      Assert.AreEqual(AxisRefType.Local, load2dFaces[2].AxisRefType);
      Assert.AreEqual(Load2dFaceType.Point, load2dFaces[2].Type);
      Assert.IsFalse(load2dFaces[2].Projected);
      Assert.AreEqual(AxisDirection3.Z, load2dFaces[2].LoadDirection);
      Assert.AreEqual(new List<double> { -10000 }, load2dFaces[2].Values);
      Assert.AreEqual(0, load2dFaces[2].R);
      Assert.AreEqual(0, load2dFaces[2].S);

      for (int i = 0; i < load2dFaces.Count(); i++)
      {
        Assert.IsTrue(load2dFaces[i].Gwa(out var gwa));
        Assert.IsTrue(load2dFaceGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaLoad2dThermal()
    {
      var load2dThermalGwas = new List<string>()
      {
        "LOAD_2D_THERMAL.2\tthermal 1\tall\t1\tCONS\t10",
        "LOAD_2D_THERMAL.2\tthermal 2\tall\t2\tDZ\t10\t0",
        "LOAD_2D_THERMAL.2\tthermal 3\tall\t3\tGEN\t10\t6\t9\t5\t8\t4\t7\t3"
      };
      var load2dThermals = new List<GsaLoad2dThermal>();
      foreach (var g in load2dThermalGwas)
      {
        var l = new GsaLoad2dThermal();
        Assert.IsTrue(l.FromGwa(g));
        load2dThermals.Add(l);
      }
      Assert.AreEqual("thermal 1", load2dThermals[0].Name);
      Assert.AreEqual(new List<string>() { }, load2dThermals[0].Entities);
      Assert.AreEqual(1, load2dThermals[0].LoadCaseIndex);
      Assert.AreEqual(Load2dThermalType.Uniform, load2dThermals[0].Type);
      Assert.AreEqual(new List<double>() { 10 }, load2dThermals[0].Values);

      Assert.AreEqual("thermal 2", load2dThermals[1].Name);
      Assert.AreEqual(new List<string>() { }, load2dThermals[1].Entities);
      Assert.AreEqual(2, load2dThermals[1].LoadCaseIndex);
      Assert.AreEqual(Load2dThermalType.Gradient, load2dThermals[1].Type);
      Assert.AreEqual(new List<double>() { 10, 0 }, load2dThermals[1].Values);

      Assert.AreEqual("thermal 3", load2dThermals[2].Name);
      Assert.AreEqual(new List<string>() { }, load2dThermals[2].Entities);
      Assert.AreEqual(3, load2dThermals[2].LoadCaseIndex);
      Assert.AreEqual(Load2dThermalType.General, load2dThermals[2].Type);
      Assert.AreEqual(new List<double>() { 10, 6, 9, 5, 8, 4, 7, 3 }, load2dThermals[2].Values);

      for (int i = 0; i < load2dThermals.Count(); i++)
      {
        Assert.IsTrue(load2dThermals[i].Gwa(out var gwa));
        Assert.IsTrue(load2dThermalGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaLoadGravitySimple()
    {
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = GSATargetLayer.Analysis;

      var gsaEls = GenerateMixedGsaEls();
      foreach (var gsaEl in gsaEls)
      {
        Assert.IsTrue(gsaEl.Gwa(out var gwa, true));
        Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gsaNodes = GenerateGsaNodes();
      foreach (var gsaNode in gsaNodes)
      {
        Assert.IsTrue(gsaNode.Gwa(out var gwa, true));
        Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gwa1 = "LOAD_GRAVITY.3\t+10% connections\tall\tall\t1\t0\t0\t-1.100000024";

      var gsaGrav1 = new GsaLoadGravity()
      {
        Index = 1,
        Name = "+10% connections",
        Entities = new List<int> { 1, 2 }, //all
        Nodes = new List<int> { 1, 2 }, //all
        LoadCaseIndex = 1,
        Z = -1.100000024
      };

      Assert.IsTrue(gsaGrav1.Gwa(out var gsaGravGwa, false));
      Assert.IsTrue(gwa1.Equals(gsaGravGwa.First()));

      Assert.IsTrue(ModelValidation(gsaGravGwa, GsaRecord.GetKeyword<GsaLoadGravity>(), 1, out var mismatch));
    }

    [Test]
    public void GsaLoadGridLineSimple()
    {
      var loadGridLineGwas = new List<string>()
      {
        "LOAD_GRID_LINE.2	loadZ	1	POLYREF	1	1	GLOBAL	NO	Z	10	15",
        "LOAD_GRID_LINE.2	loadX	2	POLYREF	1	1	GLOBAL	NO	X	10	15"
      };

      var loadGridLines = new List<GsaLoadGridLine>();
      foreach (var g in loadGridLineGwas)
      {
        var l = new GsaLoadGridLine();
        Assert.IsTrue(l.FromGwa(g));
        Assert.AreEqual(LoadLineOption.PolyRef, l.Line);
        Assert.AreEqual(1, l.PolygonIndex);
        Assert.AreEqual(1, l.LoadCaseIndex);
        Assert.AreEqual(AxisRefType.Global, l.AxisRefType);
        Assert.IsFalse(l.Projected);
        Assert.AreEqual(10, l.Value1);
        Assert.AreEqual(15, l.Value2);
        loadGridLines.Add(l);
      }

      Assert.AreEqual(AxisDirection3.Z, loadGridLines[0].LoadDirection);
      Assert.AreEqual(AxisDirection3.X, loadGridLines[1].LoadDirection);

      for (int i = 0; i < loadGridLines.Count(); i++)
      {
        Assert.IsTrue(loadGridLines[i].Gwa(out var gwa));
        Assert.IsTrue(loadGridLineGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaLoadGridPointSimple()
    {
      var loadGridPointGwas = new List<string>()
      {
        "LOAD_GRID_POINT.2	loadZ	1	10	15	1	GLOBAL	Z	20",
        "LOAD_GRID_POINT.2	loadX	1	10	15	1	GLOBAL	X	20"
      };

      var loadGridPoints = new List<GsaLoadGridPoint>();
      foreach (var g in loadGridPointGwas)
      {
        var l = new GsaLoadGridPoint();
        Assert.IsTrue(l.FromGwa(g));
        Assert.AreEqual(1, l.LoadCaseIndex);
        Assert.AreEqual(10, l.X);
        Assert.AreEqual(15, l.Y);
        Assert.AreEqual(AxisRefType.Global, l.AxisRefType);
        Assert.AreEqual(20, l.Value);
        loadGridPoints.Add(l);
      }

      Assert.AreEqual(AxisDirection3.Z, loadGridPoints[0].LoadDirection);
      Assert.AreEqual(AxisDirection3.X, loadGridPoints[1].LoadDirection);

      for (int i = 0; i < loadGridPoints.Count(); i++)
      {
        Assert.IsTrue(loadGridPoints[i].Gwa(out var gwa));
        Assert.IsTrue(loadGridPointGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaLoadNodeSimple()
    {
      var gsaObjRx = new GsaLoadNode()
      {
        ApplicationId = "AppId",
        Name = "Zero Dee Lode",
        Index = 1,
        NodeIndices = new List<int>() { 3, 4 },
        LoadCaseIndex = 3,
        GlobalAxis = true,
        LoadDirection = AxisDirection6.XX,
        Value = 23
      };

      Assert.IsTrue(gsaObjRx.Gwa(out var gwa, true));
      Assert.IsNotEmpty(gwa);
      Assert.IsTrue(ModelValidation(gwa, GsaRecord.GetKeyword<GsaLoadNode>(), 1, out var _));
    }

    [Test]
    public void GsaMatAnalSimple()
    {
      var matAnalGwas = new List<string>()
      {
        "MAT_ANAL.1\t1\tMAT_ELAS_ISO\tMaterial 1\tNO_RGB\t6\t2.05e+11\t0.3\t7850\t1.2e-05\t0\t0\t0\t0",
        "MAT_ANAL.1\t2\tMAT_ELAS_ORTHO\tMaterial 2\tNO_RGB\t14\t2.05e+11\t2.05e+11\t2.05e+11\t0.3\t0.3\t0.3\t7850\t1.2e-05\t1.2e-05\t1.2e-05\t7.8846e+10\t7.8846e+10\t7.8846e+10\t0\t0\t0",
        "MAT_ANAL.1\t3\tMAT_ELAS_PLAS_ISO\tMaterial 3\tNO_RGB\t9\t2.05e+11\t0.3\t7850\t1.2e-05\t275000000\t300000000\t0\t0\t0\t0\t0",
        "MAT_ANAL.1\t4\tMAT_MOHR_COULOMB\tMaterial 4\tNO_RGB\t9\t7.8846e+10\t0.3\t7850\t0\t0\t0\t0\t1.2e-05\t0\t0\t0",
        "MAT_ANAL.1\t5\tMAT_DRUCKER_PRAGER\tMaterial 5\tNO_RGB\t10\t7.8846e+10\t0.3\t7850\t0\t0\t0\t0\t-1\t1.2e-05\t0\t0\t0"
        //"MAT_ANAL.1\t6\tMAT_FABRIC\tMaterial 6\tNO_RGB\t5\t800000\t400000\t0.45\t30000\t0\t1\t\t0"
      };

      var matAnals = new List<GsaMatAnal>();
      foreach (var g in matAnalGwas)
      {
        var l = new GsaMatAnal();
        Assert.IsTrue(l.FromGwa(g));
        matAnals.Add(l);
      }

      #region MAT_ELAS_ISO
      Assert.AreEqual(MatAnalType.MAT_ELAS_ISO, matAnals[0].Type);
      Assert.AreEqual(2.05e+11, matAnals[0].E);
      Assert.AreEqual(0.3, matAnals[0].Nu);
      Assert.AreEqual(7850, matAnals[0].Rho);
      Assert.AreEqual(1.2e-5, matAnals[0].Alpha);
      Assert.AreEqual(0, matAnals[0].G);
      Assert.AreEqual(0, matAnals[0].Damp);
      #endregion
      #region MAT_ELAS_ORTHO
      Assert.AreEqual(MatAnalType.MAT_ELAS_ORTHO, matAnals[1].Type);
      Assert.AreEqual(2.05e+11, matAnals[1].Ex);
      Assert.AreEqual(2.05e+11, matAnals[1].Ey);
      Assert.AreEqual(2.05e+11, matAnals[1].Ez);
      Assert.AreEqual(0.3, matAnals[1].Nuxy);
      Assert.AreEqual(0.3, matAnals[1].Nuyz);
      Assert.AreEqual(0.3, matAnals[1].Nuzx);
      Assert.AreEqual(7850, matAnals[1].Rho);
      Assert.AreEqual(1.2e-5, matAnals[1].Alphax);
      Assert.AreEqual(1.2e-5, matAnals[1].Alphay);
      Assert.AreEqual(1.2e-5, matAnals[1].Alphaz);
      Assert.AreEqual(7.8846e+10, matAnals[1].Gxy);
      Assert.AreEqual(7.8846e+10, matAnals[1].Gyz);
      Assert.AreEqual(7.8846e+10, matAnals[1].Gzx);
      Assert.AreEqual(0, matAnals[1].Damp);
      #endregion
      #region MAT_ELAS_PLAS_ISO
      Assert.AreEqual(MatAnalType.MAT_ELAS_PLAS_ISO, matAnals[2].Type);
      Assert.AreEqual(2.05e+11, matAnals[2].E);
      Assert.AreEqual(0.3, matAnals[2].Nu);
      Assert.AreEqual(7850, matAnals[2].Rho);
      Assert.AreEqual(1.2e-5, matAnals[2].Alpha);
      Assert.AreEqual(275000000, matAnals[2].Yield);
      Assert.AreEqual(300000000, matAnals[2].Ultimate);
      Assert.AreEqual(0, matAnals[2].Eh);
      Assert.AreEqual(0, matAnals[2].Beta);
      Assert.AreEqual(0, matAnals[2].Damp);
      #endregion
      #region MAT_MOHR_COULOMB
      Assert.AreEqual(MatAnalType.MAT_MOHR_COULOMB, matAnals[3].Type);
      Assert.AreEqual(7.8846e+10, matAnals[3].G);
      Assert.AreEqual(0.3, matAnals[3].Nu);
      Assert.AreEqual(7850, matAnals[3].Rho);
      Assert.AreEqual(0, matAnals[3].Cohesion);
      Assert.AreEqual(0, matAnals[3].Phi);
      Assert.AreEqual(0, matAnals[3].Psi);
      Assert.AreEqual(0, matAnals[3].Eh);
      Assert.AreEqual(1.2e-5, matAnals[3].Alpha);
      Assert.AreEqual(0, matAnals[3].Damp);
      #endregion
      #region MAT_DRUCKER_PRAGER
      Assert.AreEqual(MatAnalType.MAT_DRUCKER_PRAGER, matAnals[4].Type);
      Assert.AreEqual(7.8846e+10, matAnals[4].G);
      Assert.AreEqual(0.3, matAnals[4].Nu);
      Assert.AreEqual(7850, matAnals[4].Rho);
      Assert.AreEqual(0, matAnals[4].Cohesion);
      Assert.AreEqual(0, matAnals[4].Phi);
      Assert.AreEqual(0, matAnals[4].Psi);
      Assert.AreEqual(0, matAnals[4].Eh);
      Assert.AreEqual(-1, matAnals[4].Scribe);
      Assert.AreEqual(1.2e-5, matAnals[4].Alpha);
      Assert.AreEqual(0, matAnals[4].Damp);
      #endregion

      for (int i = 0; i < matAnals.Count(); i++)
      {
        Assert.IsTrue(matAnals[i].Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("205000000000", "2.05e+11");
        gwa[0] = gwa[0].Replace("78846000000", "7.8846e+10");
        gwa[0] = gwa[0].Replace("1.2E-05", "1.2e-05");

        //compare with original gwa string
        Assert.IsTrue(matAnalGwas[i].Equals(gwa.First()));
      }
    }

    [Ignore("Bugs identified in keyword definition documentation")]
    [Test]
    public void GsaMatConcreteSimple()
    {
      var matConcreteGwas = new List<string>()
      {
        "MAT_CONCRETE.17\t1\tMAT.10\t40 MPa\t3.315274903e+10\t40000000\t0.2\t1.381364543e+10\t2400\t1e-05\tMAT_ANAL.1\tConcrete\t-268435456\tMAT_ELAS_ISO\t6\t3.315274903e+10\t0.2\t2400\t1e-05\t1.381364543e+10\t0\t0\t0\t0\t0\t0\t0\t0\tMAT_CURVE_PARAM.3\t\tRECTANGLE+NO_TENSION\t0.00068931\t0\t0.00069069\t0\t0.003\t1\t1\t1\tMAT_CURVE_PARAM.3\t\tLINEAR+INTERPOLATED\t0.003\t0\t0.003\t0\t0.003\t0.0001144620975\t1\t1\t0\tConcrete\tCYLINDER\tN\t40000000\t34000000\t16000000\t3794733.192\t2276839.915\t0\t1\t2\t0.003\t0.003\t0.00069\t0.003\t0.0025\t0.002\t0.0025\tNO\t0.02\t0\t1\t0.77\t0\t0\t0\t0\t0"
      };
      var matConcretes = new List<GsaMatConcrete>();
      foreach (var g in matConcreteGwas)
      {
        var l = new GsaMatConcrete();
        Assert.IsTrue(l.FromGwa(g));
        matConcretes.Add(l);
      }
      Assert.AreEqual(2e11, matConcretes[0].Mat.E);
      Assert.AreEqual(360000000, matConcretes[0].Mat.F);
      Assert.AreEqual(0.3, matConcretes[0].Mat.Nu);
      Assert.AreEqual(7.692307692e+10, matConcretes[0].Mat.G);
      Assert.AreEqual(7850, matConcretes[0].Mat.Rho);
      Assert.AreEqual(1.2e-05, matConcretes[0].Mat.Alpha);
      Assert.AreEqual(MatAnalType.MAT_ELAS_ISO, matConcretes[0].Mat.Prop.Type);
      Assert.AreEqual(6, matConcretes[0].Mat.Prop.NumParams);
      Assert.AreEqual(2e11, matConcretes[0].Mat.Prop.E);
      Assert.AreEqual(0.3, matConcretes[0].Mat.Prop.Nu);
      Assert.AreEqual(7850, matConcretes[0].Mat.Prop.Rho);
      Assert.AreEqual(1.2e-5, matConcretes[0].Mat.Prop.Alpha);
      Assert.AreEqual(7.692307692e+10, matConcretes[0].Mat.Prop.G);
      Assert.AreEqual(0, matConcretes[0].Mat.Prop.Damp);
      Assert.AreEqual(0, matConcretes[0].Mat.NumUC);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.AbsUC);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.OrdUC);
      Assert.AreEqual(new double[0], matConcretes[0].Mat.PtsUC);
      Assert.AreEqual(0, matConcretes[0].Mat.NumSC);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.AbsSC);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.OrdSC);
      Assert.AreEqual(new double[0], matConcretes[0].Mat.PtsSC);
      Assert.AreEqual(0, matConcretes[0].Mat.NumUT);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.AbsUT);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.OrdUT);
      Assert.AreEqual(new double[0], matConcretes[0].Mat.PtsUT);
      Assert.AreEqual(0, matConcretes[0].Mat.NumST);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.AbsST);
      Assert.AreEqual(Dimension.NotSet, matConcretes[0].Mat.OrdST);
      Assert.AreEqual(new double[0], matConcretes[0].Mat.PtsST);
      Assert.AreEqual(0.05, matConcretes[0].Mat.Eps);
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.UNDEF }, matConcretes[0].Mat.Uls.Model);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Uls.StrainElasticCompression);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Uls.StrainElasticTension);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Uls.StrainPlasticCompression);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Uls.StrainPlasticTension);
      Assert.AreEqual(0.05, matConcretes[0].Mat.Uls.StrainFailureCompression);
      Assert.AreEqual(0.05, matConcretes[0].Mat.Uls.StrainFailureTension);
      Assert.AreEqual(1, matConcretes[0].Mat.Uls.GammaF);
      Assert.AreEqual(1, matConcretes[0].Mat.Uls.GammaE);
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS }, matConcretes[0].Mat.Sls.Model);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Sls.StrainElasticCompression);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Sls.StrainElasticTension);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Sls.StrainPlasticCompression);
      Assert.AreEqual(0.0018, matConcretes[0].Mat.Sls.StrainPlasticTension);
      Assert.AreEqual(0.05, matConcretes[0].Mat.Sls.StrainFailureCompression);
      Assert.AreEqual(0.05, matConcretes[0].Mat.Sls.StrainFailureTension);
      Assert.AreEqual(1, matConcretes[0].Mat.Sls.GammaF);
      Assert.AreEqual(1, matConcretes[0].Mat.Sls.GammaE);
      Assert.AreEqual(0, matConcretes[0].Mat.Cost);
      Assert.AreEqual(MatType.STEEL, matConcretes[0].Mat.Type);
      Assert.AreEqual(0, matConcretes[0].Fc);
      Assert.AreEqual(0, matConcretes[0].Fcd);
      Assert.AreEqual(0, matConcretes[0].Fcdc);
      Assert.AreEqual(0, matConcretes[0].Fcdt);
      Assert.AreEqual(0, matConcretes[0].Fcfib);
      Assert.AreEqual(0, matConcretes[0].EmEs);
      Assert.AreEqual(0, matConcretes[0].N);
      Assert.AreEqual(0, matConcretes[0].Emod);
      Assert.AreEqual(0, matConcretes[0].EpsPeak);
      Assert.AreEqual(0, matConcretes[0].EpsMax);
      Assert.AreEqual(0, matConcretes[0].EpsU);
      Assert.AreEqual(0, matConcretes[0].EpsAx);
      Assert.AreEqual(0, matConcretes[0].EpsTran);
      Assert.AreEqual(0, matConcretes[0].EpsAxs);
      Assert.AreEqual(0, matConcretes[0].Light);
      Assert.AreEqual(0, matConcretes[0].Agg);
      Assert.AreEqual(0, matConcretes[0].XdMin);
      Assert.AreEqual(0, matConcretes[0].XdMax);
      Assert.AreEqual(0, matConcretes[0].Beta);
      Assert.AreEqual(0, matConcretes[0].Shrink);
      Assert.AreEqual(0, matConcretes[0].Confine);
      Assert.AreEqual(0, matConcretes[0].Fcc);
      Assert.AreEqual(0, matConcretes[0].EpsPlasC);
      Assert.AreEqual(0, matConcretes[0].EpsUC);

      for (int i = 0; i < matConcretes.Count(); i++)
      {
        Assert.IsTrue(matConcretes[i].Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("33152749030", "3.315274903e+10");
        gwa[0] = gwa[0].Replace("13813645430", "1.381364543e+10");
        gwa[0] = gwa[0].Replace("1E-05", "1e-05");
        gwa[0] = gwa[0].Replace("\tCONCRETE", "\tConcrete");

        //compare with original gwa string
        Assert.IsTrue(matConcreteGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaMatCurveParamSimple()
    {
      var matCurveParamGwas = new List<string>()
      {
        "MAT_CURVE_PARAM.3\t\tRECTANGLE+EXPLICIT\t0.00041083875\t0\t0.00041166125\t0\t0.003\t0\t1\t1",
        "MAT_CURVE_PARAM.3\t\tEXPLICIT\t0\t0\t0\t0\t0\t0\t1\t1",
        "MAT_CURVE_PARAM.3\t\tUNDEF\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1",
        "MAT_CURVE_PARAM.3\t\tELAS_PLAS\t0.0016\t0.0016\t0.0016\t0.0016\t0.05\t0.05\t1\t1",
        "MAT_CURVE_PARAM.3\t\tRECTANGLE+NO_TENSION\t0.00068931\t0\t0.00069069\t0\t0.003\t1\t1\t1",
        "MAT_CURVE_PARAM.3\t\tLINEAR+INTERPOLATED\t0.003\t0\t0.003\t0\t0.003\t0.0001144620975\t1\t1"
      };

      var matCurveParams = new List<GsaMatCurveParam>();
      foreach (var g in matCurveParamGwas)
      {
        var l = new GsaMatCurveParam();
        Assert.IsTrue(l.FromGwa(g));
        matCurveParams.Add(l);
      }

      #region curve 1
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.EXPLICIT }, matCurveParams[0].Model);
      Assert.AreEqual(0.00041083875, matCurveParams[0].StrainElasticCompression);
      Assert.AreEqual(0, matCurveParams[0].StrainElasticTension);
      Assert.AreEqual(0.00041166125, matCurveParams[0].StrainPlasticCompression);
      Assert.AreEqual(0, matCurveParams[0].StrainPlasticTension);
      Assert.AreEqual(0.003, matCurveParams[0].StrainFailureCompression);
      Assert.AreEqual(0, matCurveParams[0].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[0].GammaF);
      Assert.AreEqual(1, matCurveParams[0].GammaE);
      #endregion
      #region curve 2
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.EXPLICIT }, matCurveParams[1].Model);
      Assert.AreEqual(0, matCurveParams[1].StrainElasticCompression);
      Assert.AreEqual(0, matCurveParams[1].StrainElasticTension);
      Assert.AreEqual(0, matCurveParams[1].StrainPlasticCompression);
      Assert.AreEqual(0, matCurveParams[1].StrainPlasticTension);
      Assert.AreEqual(0, matCurveParams[1].StrainFailureCompression);
      Assert.AreEqual(0, matCurveParams[1].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[1].GammaF);
      Assert.AreEqual(1, matCurveParams[1].GammaE);
      #endregion
      #region curve 3
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.UNDEF }, matCurveParams[2].Model);
      Assert.AreEqual(0.0018, matCurveParams[2].StrainElasticCompression);
      Assert.AreEqual(0.0018, matCurveParams[2].StrainElasticTension);
      Assert.AreEqual(0.0018, matCurveParams[2].StrainPlasticCompression);
      Assert.AreEqual(0.0018, matCurveParams[2].StrainPlasticTension);
      Assert.AreEqual(0.05, matCurveParams[2].StrainFailureCompression);
      Assert.AreEqual(0.05, matCurveParams[2].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[2].GammaF);
      Assert.AreEqual(1, matCurveParams[2].GammaE);
      #endregion
      #region curve 4
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS }, matCurveParams[3].Model);
      Assert.AreEqual(0.0016, matCurveParams[3].StrainElasticCompression);
      Assert.AreEqual(0.0016, matCurveParams[3].StrainElasticTension);
      Assert.AreEqual(0.0016, matCurveParams[3].StrainPlasticCompression);
      Assert.AreEqual(0.0016, matCurveParams[3].StrainPlasticTension);
      Assert.AreEqual(0.05, matCurveParams[3].StrainFailureCompression);
      Assert.AreEqual(0.05, matCurveParams[3].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[3].GammaF);
      Assert.AreEqual(1, matCurveParams[3].GammaE);
      #endregion
      #region curve 5
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION }, matCurveParams[4].Model);
      Assert.AreEqual(0.00068931, matCurveParams[4].StrainElasticCompression);
      Assert.AreEqual(0, matCurveParams[4].StrainElasticTension);
      Assert.AreEqual(0.00069069, matCurveParams[4].StrainPlasticCompression);
      Assert.AreEqual(0, matCurveParams[4].StrainPlasticTension);
      Assert.AreEqual(0.003, matCurveParams[4].StrainFailureCompression);
      Assert.AreEqual(1, matCurveParams[4].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[4].GammaF);
      Assert.AreEqual(1, matCurveParams[4].GammaE);
      #endregion
      #region curve 6
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.LINEAR, MatCurveParamType.INTERPOLATED }, matCurveParams[5].Model);
      Assert.AreEqual(0.003, matCurveParams[5].StrainElasticCompression);
      Assert.AreEqual(0, matCurveParams[5].StrainElasticTension);
      Assert.AreEqual(0.003, matCurveParams[5].StrainPlasticCompression);
      Assert.AreEqual(0, matCurveParams[5].StrainPlasticTension);
      Assert.AreEqual(0.003, matCurveParams[5].StrainFailureCompression);
      Assert.AreEqual(0.0001144620975, matCurveParams[5].StrainFailureTension);
      Assert.AreEqual(1, matCurveParams[5].GammaF);
      Assert.AreEqual(1, matCurveParams[5].GammaE);
      #endregion

      for (int i = 0; i < matCurveParams.Count(); i++)
      {
        Assert.IsTrue(matCurveParams[i].Gwa(out var gwa));
        Assert.IsTrue(matCurveParamGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaMatCurveSimple()
    {
      var matCurveGwas = new List<string>()
      {
        "MAT_CURVE.1\t1\tMaterial Curve 1\tDISP\tFORCE\t\"(1,10) (2,20) (3,30)\""
      };

      var matCurves = new List<GsaMatCurve>();
      foreach (var g in matCurveGwas)
      {
        var l = new GsaMatCurve();
        Assert.IsTrue(l.FromGwa(g));
        matCurves.Add(l);
      }

      Assert.AreEqual(Dimension.DISP, matCurves[0].Abscissa);
      Assert.AreEqual(Dimension.FORCE, matCurves[0].Ordinate);
      Assert.AreEqual(new double[3, 2] { { 1, 10 }, { 2, 20 }, { 3, 30 } }, matCurves[0].Table);

      for (int i = 0; i < matCurves.Count(); i++)
      {
        Assert.IsTrue(matCurves[i].Gwa(out var gwa));
        Assert.IsTrue(matCurveGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaMatSteelSimple()
    {
      var matSteelGwas = new List<string>()
      {
        "MAT_STEEL.3\t1\tMAT.10\t350(AS3678)\t2e+11\t360000000\t0.3\t7.692307692e+10\t7850\t1.2e-05\tMAT_ANAL.1\tSteel\t-268435456\tMAT_ELAS_ISO\t6\t2e+11\t0.3\t7850\t1.2e-05\t7.692307692e+10\t0\t0\t0\t0\t0\t0\t0\t0.05\tMAT_CURVE_PARAM.3\t\tUNDEF\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1\tMAT_CURVE_PARAM.3\t\tELAS_PLAS\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1\t0\tSteel\t360000000\t450000000\t0\t0"
      };
      var matSteels = new List<GsaMatSteel>();
      foreach (var g in matSteelGwas)
      {
        var l = new GsaMatSteel();
        Assert.IsTrue(l.FromGwa(g));
        matSteels.Add(l);
      }
      Assert.AreEqual(2e11, matSteels[0].Mat.E);
      Assert.AreEqual(360000000, matSteels[0].Mat.F);
      Assert.AreEqual(0.3, matSteels[0].Mat.Nu);
      Assert.AreEqual(7.692307692e+10, matSteels[0].Mat.G);
      Assert.AreEqual(7850, matSteels[0].Mat.Rho);
      Assert.AreEqual(1.2e-05, matSteels[0].Mat.Alpha);
      Assert.AreEqual(MatAnalType.MAT_ELAS_ISO, matSteels[0].Mat.Prop.Type);
      Assert.AreEqual(6, matSteels[0].Mat.Prop.NumParams);
      Assert.AreEqual(2e11, matSteels[0].Mat.Prop.E);
      Assert.AreEqual(0.3, matSteels[0].Mat.Prop.Nu);
      Assert.AreEqual(7850, matSteels[0].Mat.Prop.Rho);
      Assert.AreEqual(1.2e-5, matSteels[0].Mat.Prop.Alpha);
      Assert.AreEqual(7.692307692e+10, matSteels[0].Mat.Prop.G);
      Assert.AreEqual(0, matSteels[0].Mat.Prop.Damp);
      Assert.AreEqual(0, matSteels[0].Mat.NumUC);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.AbsUC);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.OrdUC);
      Assert.AreEqual(new double[0], matSteels[0].Mat.PtsUC);
      Assert.AreEqual(0, matSteels[0].Mat.NumSC);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.AbsSC);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.OrdSC);
      Assert.AreEqual(new double[0], matSteels[0].Mat.PtsSC);
      Assert.AreEqual(0, matSteels[0].Mat.NumUT);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.AbsUT);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.OrdUT);
      Assert.AreEqual(new double[0], matSteels[0].Mat.PtsUT);
      Assert.AreEqual(0, matSteels[0].Mat.NumST);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.AbsST);
      Assert.AreEqual(Dimension.NotSet, matSteels[0].Mat.OrdST);
      Assert.AreEqual(new double[0], matSteels[0].Mat.PtsST);
      Assert.AreEqual(0.05, matSteels[0].Mat.Eps);
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.UNDEF }, matSteels[0].Mat.Uls.Model);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Uls.StrainElasticCompression);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Uls.StrainElasticTension);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Uls.StrainPlasticCompression);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Uls.StrainPlasticTension);
      Assert.AreEqual(0.05, matSteels[0].Mat.Uls.StrainFailureCompression);
      Assert.AreEqual(0.05, matSteels[0].Mat.Uls.StrainFailureTension);
      Assert.AreEqual(1, matSteels[0].Mat.Uls.GammaF);
      Assert.AreEqual(1, matSteels[0].Mat.Uls.GammaE);
      Assert.AreEqual(new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS }, matSteels[0].Mat.Sls.Model);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Sls.StrainElasticCompression);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Sls.StrainElasticTension);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Sls.StrainPlasticCompression);
      Assert.AreEqual(0.0018, matSteels[0].Mat.Sls.StrainPlasticTension);
      Assert.AreEqual(0.05, matSteels[0].Mat.Sls.StrainFailureCompression);
      Assert.AreEqual(0.05, matSteels[0].Mat.Sls.StrainFailureTension);
      Assert.AreEqual(1, matSteels[0].Mat.Sls.GammaF);
      Assert.AreEqual(1, matSteels[0].Mat.Sls.GammaE);
      Assert.AreEqual(0, matSteels[0].Mat.Cost);
      Assert.AreEqual(MatType.STEEL, matSteels[0].Mat.Type);
      Assert.AreEqual(360000000, matSteels[0].Fy);
      Assert.AreEqual(450000000, matSteels[0].Fu);
      Assert.AreEqual(0, matSteels[0].EpsP);
      Assert.AreEqual(0, matSteels[0].Eh);

      for (int i = 0; i < matSteels.Count(); i++)
      {
        Assert.IsTrue(matSteels[i].Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("200000000000", "2e+11");
        gwa[0] = gwa[0].Replace("76923076920", "7.692307692e+10");
        gwa[0] = gwa[0].Replace("1.2E-05", "1.2e-05");
        gwa[0] = gwa[0].Replace("\tSTEEL", "\tSteel");

        //compare with original gwa string
        Assert.IsTrue(matSteelGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaMemb1dSimple()
    {
      //An asterisk next to a row signifies non-obvious values I've specifically changed between all 3 (obvious values are application ID, index and name)

      var gsaMembBeamAuto = new GsaMemb()
      {
        ApplicationId = "beamauto",
        Name = "Beam Auto",
        Index = 1,
        Type = MemberType.Beam, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 3,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BEAM, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.X, ReleaseCode.Released }, { AxisDirection6.XX, ReleaseCode.Released } }, //*
        Releases2 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released } }, //*
        RestraintEnd1 = Restraint.Fixed, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Automatic, //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.TopFlange, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembBeamAuto.Gwa(out var gwa1, false));

      var gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa1.First()));
      gsaMembBeamAuto.ShouldDeepEqual(gsaMemb);

      var gsaMembColEffLen = new GsaMemb()
      {
        ApplicationId = "efflencol",
        Name = "Eff Len Col",
        Index = 2,
        Type = MemberType.Column, //*
        Exposure = ExposedSurfaces.ONE, //*
        PropertyIndex = 3,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BAR, //*
        Fire = FireResistance.FourHours,//*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.EffectiveLength, //*
        EffectiveLengthYY = 18, //*
        PercentageZZ = 65, //*
        EffectiveLengthLateralTorsional = 19, //*
        LoadHeight = 19, //*
        LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembColEffLen.Gwa(out var gwa2, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa2.First()));
      gsaMembColEffLen.ShouldDeepEqual(gsaMemb);

      var gsaMembGeneric1dExplicit = new GsaMemb()
      {
        ApplicationId = "explicitcol",
        Name = "Explicit Generic 1D",
        Index = 3,
        Type = MemberType.Generic1d, //*
        Exposure = ExposedSurfaces.NONE, //*
        PropertyIndex = 3,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.DAMPER, //*
        Fire = FireResistance.FourHours, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Explicit, //*
        PointRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { All = true, Restraint = Restraint.TopFlangeLateral }
        },  //*
        SpanRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { Index = 1, Restraint = Restraint.Fixed },
          new RestraintDefinition() { Index = 3, Restraint = Restraint.PartialRotational }
        },  //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.BottomFlange, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembGeneric1dExplicit.Gwa(out var gwa3, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa3.First()));
      gsaMembGeneric1dExplicit.ShouldDeepEqual(gsaMemb);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Test]
    public void GsaMemb2dSimple()
    {
      var gsaMembSlabLinear = new GsaMemb()
      {
        ApplicationId = "slablinear",
        Name = "Slab Linear",
        Index = 1,
        Type = MemberType.Slab, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 2,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.LINEAR, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembSlabLinear.Gwa(out var gwa1, false));

      var gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa1.First()));
      gsaMembSlabLinear.ShouldDeepEqual(gsaMemb);

      var gsaMembWallQuadratic = new GsaMemb()
      {
        ApplicationId = "wallquadratic",
        Name = "Wall Quadratic",
        Index = 2,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.QUADRATIC, //*
        Fire = FireResistance.ThreeHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembWallQuadratic.Gwa(out var gwa2, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa2.First()));
      gsaMembWallQuadratic.ShouldDeepEqual(gsaMemb);

      var gsaMembGeneric = new GsaMemb()
      {
        ApplicationId = "generic2dRigid",
        Name = "Wall XY Rigid Diaphragm",
        Index = 3,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.RIGID, //*
        Fire = FireResistance.TwoHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembGeneric.Gwa(out var gwa3, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa3.First()));
      gsaMembGeneric.ShouldDeepEqual(gsaMemb);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Test]
    public void GsaNodeSimple()
    {
      var nodeGwas = new List<string>()
      {
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8",
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8\tfree\tGLOBAL\t0\t1\t2",
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8\txz\tGLOBAL\t45\t1\t2"
      };
      var nodes = new List<GsaNode>();
      foreach (var g in nodeGwas)
      {
        var n = new GsaNode();
        Assert.IsTrue(n.FromGwa(g));
        Assert.AreEqual(628.3, n.X);
        Assert.AreEqual(-107, n.Y);
        Assert.AreEqual(222.8, n.Z);
        nodes.Add(n);
      }

      Assert.AreEqual(NodeRestraint.Free, nodes[0].NodeRestraint);
      Assert.IsTrue(nodes[0].Restraints == null || nodes[0].Restraints.Count() == 0);
      Assert.IsNull(nodes[0].SpringPropertyIndex);
      Assert.IsNull(nodes[0].MassPropertyIndex);

      Assert.AreEqual(NodeRestraint.Free, nodes[1].NodeRestraint);
      Assert.IsTrue(nodes[1].Restraints == null || nodes[1].Restraints.Count() == 0);
      Assert.AreEqual(1, nodes[1].SpringPropertyIndex);
      Assert.AreEqual(2, nodes[1].MassPropertyIndex);

      Assert.AreEqual(NodeRestraint.Custom, nodes[2].NodeRestraint);
      Assert.IsTrue(nodes[2].Restraints.SequenceEqual(new AxisDirection6[] { AxisDirection6.X, AxisDirection6.Z }));
      Assert.AreEqual(45, nodes[2].MeshSize);

      for (int i = 0; i < nodes.Count(); i++)
      {
        Assert.IsTrue(nodes[i].Gwa(out var gwa));
        Assert.IsTrue(nodeGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaPathSimple()
    {
      var pathGwas = new List<string>()
      {
        "PATH.1\t1\tLeft Lane\tLANE\t1\t1\t-4\t-1\t0.5\t0",
        "PATH.1\t2\tRight Lane\tLANE\t1\t1\t-7\t-4\t0.5\t0",
        "PATH.1\t3\trailway\tTRACK\t2\t1\t-8\t1.434999943\t0.5\t0"
      };
      var paths = new List<GsaPath>();
      foreach (var g in pathGwas)
      {
        var l = new GsaPath();
        Assert.IsTrue(l.FromGwa(g));
        paths.Add(l);
      }
      Assert.AreEqual(PathType.LANE, paths[0].Type);
      Assert.AreEqual(1, paths[0].Group);
      Assert.AreEqual(1, paths[0].Alignment);
      Assert.AreEqual(-4, paths[0].Left);
      Assert.AreEqual(-1, paths[0].Right);
      Assert.AreEqual(0.5, paths[0].Factor);
      Assert.AreEqual(0, paths[0].NumMarkedLanes);

      Assert.AreEqual(PathType.LANE, paths[1].Type);
      Assert.AreEqual(1, paths[1].Group);
      Assert.AreEqual(1, paths[1].Alignment);
      Assert.AreEqual(-7, paths[1].Left);
      Assert.AreEqual(-4, paths[1].Right);
      Assert.AreEqual(0.5, paths[1].Factor);
      Assert.AreEqual(0, paths[1].NumMarkedLanes);

      Assert.AreEqual(PathType.TRACK, paths[2].Type);
      Assert.AreEqual(2, paths[2].Group);
      Assert.AreEqual(1, paths[2].Alignment);
      Assert.AreEqual(-8, paths[2].Left);
      Assert.AreEqual(1.434999943, paths[2].Right);
      Assert.AreEqual(0.5, paths[2].Factor);
      Assert.AreEqual(0, paths[2].NumMarkedLanes);

      for (int i = 0; i < paths.Count(); i++)
      {
        Assert.IsTrue(paths[i].Gwa(out var gwa));
        Assert.IsTrue(pathGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaProp2dSimple()
    {
      var supportedProp2dGwa = "PROP_2D.7\t1\tSlab property\tNO_RGB\tSHELL\tGLOBAL\t0\tCONCRETE\t1\t0\t0.3\tCENTROID\t0\t0\t100%\t100%\t100%\t100%";
      var unsupportedProp2dGwa = "PROP_2D.7\t1\tSlab property\tNO_RGB\tCURVED\tGLOBAL\t0\tCONCRETE\t1\t0\t0.3(m) D 0 CAT RLD Ribdeck AL (0.9)\tCENTROID\t0\t0\t100%\t100%\t100%\t100%";

      var p1 = new GsaProp2d();
      Assert.IsTrue(p1.FromGwa(supportedProp2dGwa));
      var p2 = new GsaProp2d();
      Assert.IsTrue(p2.FromGwa(unsupportedProp2dGwa));

      Assert.AreEqual(0.3, p1.Thickness);
      Assert.AreEqual(Property2dRefSurface.Centroid, p1.RefPt);

      Assert.IsTrue(p1.Gwa(out var gwa));
      Assert.IsTrue(supportedProp2dGwa.Equals(gwa.First()));

      Assert.IsTrue(ModelValidation(supportedProp2dGwa, GsaRecord.GetKeyword<GsaProp2d>(), 1, out var _));
    }

    [Test]
    public void GsaPropMassSimple()
    {
      var massGwas = new List<string>()
      {
        "PROP_MASS.3\t1\tMass prop. 1\tNO_RGB\t4\t0\t0\t0\t0\t0\t0\tMOD\t100%\t100%\t100%"
      };
      var masses = new List<GsaPropMass>();
      foreach (var g in massGwas)
      {
        var m = new GsaPropMass();
        Assert.IsTrue(m.FromGwa(g));
        masses.Add(m);
      }

      for (int i = 0; i < masses.Count(); i++)
      {
        Assert.IsTrue(masses[i].Gwa(out var gwa));
        Assert.IsTrue(massGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaPropSprSimple()
    {
      var propGwas = new List<string>()
      {
        "PROP_SPR.4\t1\tLSPxGeneral\tNO_RGB\tGENERAL\t0\t12\t0\t15\t0\t20\t0\t25\t0\t30\t0\t38\t0.21",
        "PROP_SPR.4\t2\tLSPxAxial\tNO_RGB\tAXIAL\t12\t0.21",
        "PROP_SPR.4\t3\tLSPxTorsional\tNO_RGB\tTORSIONAL\t12\t0.21",
        "PROP_SPR.4\t4\tLSPxCompression\tNO_RGB\tCOMPRESSION\t12\t0.21",
        "PROP_SPR.4\t5\tLSPxTension\tNO_RGB\tTENSION\t12\t0.21",
        "PROP_SPR.4\t6\tLSPxLockup\tNO_RGB\tLOCKUP\t12\t0.21\t0\t0",
        "PROP_SPR.4\t7\tLSPxGap\tNO_RGB\tGAP\t12\t0.21",
        "PROP_SPR.4\t8\tLSPxFriction\tNO_RGB\tFRICTION\t12\t15\t20\t0\t0.21"
      };
      var props = new List<GsaPropSpr>();
      foreach (var g in propGwas)
      {
        var p = new GsaPropSpr();
        Assert.IsTrue(p.FromGwa(g));
        Assert.AreEqual(0.21, p.DampingRatio, 0.0001);
        Assert.AreEqual(12, p.Stiffnesses[p.Stiffnesses.Keys.First()]);
        props.Add(p);
      }

      Assert.AreEqual(25, props[0].Stiffnesses[AxisDirection6.XX]);
      Assert.AreEqual(38, props[0].Stiffnesses[AxisDirection6.ZZ]);

      Assert.AreEqual(12, props[2].Stiffnesses[AxisDirection6.XX]);

      Assert.AreEqual(12, props[7].Stiffnesses[AxisDirection6.X]);
      Assert.AreEqual(15, props[7].Stiffnesses[AxisDirection6.Y]);
      Assert.AreEqual(20, props[7].Stiffnesses[AxisDirection6.Z]);


      for (int i = 0; i < props.Count(); i++)
      {
        Assert.IsTrue(props[i].Gwa(out var gwa));
        Assert.IsTrue(propGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaRigidSimple()
    {
      var rigidGwas = new List<string>()
      {
        "RIGID.3\t\t71\tALL\t73 75 77 79 81 83\t1 2\t0",
        "RIGID.3\t\t71\tXY_PLANE\t73 75 77 79 81 83\t1\t0",
        "RIGID.3\t\t71\tX:XYYZZ-Y:YZZ-YY:YY-ZZ:ZZ\t73 75 77 79 81 83\t2\t0",
      };

      var rigids = new List<GsaRigid>();
      foreach (var g in rigidGwas)
      {
        var l = new GsaRigid();
        Assert.IsTrue(l.FromGwa(g));
        rigids.Add(l);
      }

      Assert.AreEqual(71, rigids[0].PrimaryNode);
      Assert.AreEqual(RigidConstraintType.ALL, rigids[0].Type);
      Assert.IsNull(rigids[0].Link);
      Assert.AreEqual(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[0].ConstrainedNodes);
      Assert.AreEqual(new List<int>() { 1, 2 }, rigids[0].Stage);
      Assert.AreEqual(0, rigids[0].ParentMember);

      Assert.AreEqual(71, rigids[1].PrimaryNode);
      Assert.AreEqual(RigidConstraintType.XY_PLANE, rigids[1].Type);
      Assert.IsNull(rigids[1].Link);
      Assert.AreEqual(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[1].ConstrainedNodes);
      Assert.AreEqual(new List<int>() { 1 }, rigids[1].Stage);
      Assert.AreEqual(0, rigids[1].ParentMember);

      Assert.AreEqual(71, rigids[2].PrimaryNode);
      Assert.AreEqual(RigidConstraintType.Custom, rigids[2].Type);
      Assert.AreEqual(new List<AxisDirection6> { AxisDirection6.X, AxisDirection6.YY, AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.X]);
      Assert.AreEqual(new List<AxisDirection6> { AxisDirection6.Y, AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.Y]);
      Assert.AreEqual(new List<AxisDirection6> { AxisDirection6.YY }, rigids[2].Link[AxisDirection6.YY]);
      Assert.AreEqual(new List<AxisDirection6> { AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.ZZ]);
      Assert.AreEqual(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[2].ConstrainedNodes);
      Assert.AreEqual(new List<int>() { 2 }, rigids[2].Stage);
      Assert.AreEqual(0, rigids[1].ParentMember);

      for (int i = 0; i < rigids.Count(); i++)
      {
        Assert.IsTrue(rigids[i].Gwa(out var gwa));
        Assert.IsTrue(rigidGwas[i].Equals(gwa.First()));
      }
    }

    [Test]
    public void GsaSectionSimple()
    {
      var gwa1 = "SECTION.7\t3\tNO_RGB\tSTD GZ 10 3 3 1.5 1.6 1\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tSTD GZ 10 3 3 1.5 1.6 1\t0\t0\t0\tY_AXIS\t0\tNONE\t0\t0\t0\tNO_ENVIRON";
      var gwa2 = "SECTION.7\t2\tNO_RGB\t150x150x12EA-BtB\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t1\tSTEEL\t1\tSTD D 150 150 12 12\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tROLLED\tUNDEF\t0\t0\tNO_ENVIRON";
      var gwa3 = "SECTION.7\t7\tNO_RGB\tfgds\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t2\tCONCRETE\t1\tSTD CH 99 60 8 9\t0\t0\t0\tNONE\t0\tSIMPLE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t89.99999998\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t0\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON";
      var gwaExp = "SECTION.7\t2\tNO_RGB\tEXP 1 2 3 4 5 6\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tEXP 1 2 3 4 5 6\t0\t0\t0\tNONE\t0\tNONE\t0\t0\t0\tNO_ENVIRON";

      var gsaSection1 = new GsaSection();
      gsaSection1.FromGwa(gwa1);
      gsaSection1.Gwa(out var gwaOut1);
      Assert.IsTrue(gwa1.Equals(gwaOut1.First(), StringComparison.InvariantCulture));

      var gsaSection2 = new GsaSection();
      gsaSection2.FromGwa(gwa2);
      gsaSection2.Gwa(out var gwaOut2);
      Assert.IsTrue(gwa2.Equals(gwaOut2.First(), StringComparison.InvariantCulture));

      var gsaSection3 = new GsaSection();
      gsaSection3.FromGwa(gwa3);
      gsaSection3.Gwa(out var gwaOut3);
      Assert.IsTrue(gwa3.Equals(gwaOut3.First(), StringComparison.InvariantCulture));

      var gsaSectionExp = new GsaSection();
      gsaSectionExp.FromGwa(gwaExp);
      gsaSectionExp.Gwa(out var gwaOutExp);
      Assert.IsTrue(gwaExp.Equals(gwaOutExp.First(), StringComparison.InvariantCulture));
    }

    [Test]
    public void GsaUserVehicleSimple()
    {
      var userVehicleGwas = new List<string>()
      {
        "USER_VEHICLE.1\t1\tVehicle 1\t1\t3\t1\t1\t1\t1\t2\t1\t1\t1\t3\t1\t1\t1"
      };
      var userVehicles = new List<GsaUserVehicle>();
      foreach (var g in userVehicleGwas)
      {
        var l = new GsaUserVehicle();
        Assert.IsTrue(l.FromGwa(g));
        userVehicles.Add(l);
      }
      Assert.AreEqual(1, userVehicles[0].Width);
      Assert.AreEqual(3, userVehicles[0].NumAxle);
      Assert.AreEqual(new List<double>() { 1, 2, 3 }, userVehicles[0].AxlePosition);
      Assert.AreEqual(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleOffset);
      Assert.AreEqual(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleLeft);
      Assert.AreEqual(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleRight);

      for (int i = 0; i < userVehicles.Count(); i++)
      {
        Assert.IsTrue(userVehicles[i].Gwa(out var gwa));
        Assert.IsTrue(userVehicleGwas[i].Equals(gwa.First()));
      }
    }
    #endregion

    #region ToNative
    //Note for understanding:
    //StructuralStorey <-> GRID_PLANE
    //StructuralLoadPlane <-> GRID_SURFACE, which references GRID_PLANEs
    [Test]
    public void GsaLoadPanelHierarchyToNativeTest()
    {
      var gsaElevatedAxis = new GsaAxis() { Index = 1, ApplicationId = "Axis1", Name = "StandardAxis", XDirX = 1, XDirY = 0, XDirZ = 0, XYDirX = 0, XYDirY = 1, XYDirZ = 0, OriginX = 10, OriginY = 20, OriginZ = 30 };
      var gsaRotatedAxis = new GsaAxis() { Index = 2, ApplicationId = "Axis2", Name = "AngledAxis", XDirX = 1, XDirY = 1, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };

      var storey1 = new StructuralStorey()
      {
        ApplicationId = "TestStorey",
        Name = "Test Storey",
        Axis = (StructuralAxis)gsaRotatedAxis.ToSpeckle(),
        Elevation = 10,
        ToleranceAbove = 5,
        ToleranceBelow = 6
      };
      StructuralStoreyToNative.ToNative(storey1).Split('\n');

      //Without storey reference, but with an axis (must have one or the other) - should be written as GLOBAL
      //There is no way currently to specify X elevation, Y elevation etc
      var plane1 = new StructuralLoadPlane()
      {
        ApplicationId = "lp1",
        Axis = (StructuralAxis)gsaElevatedAxis.ToSpeckle(),
        ElementDimension = 2,
        Tolerance = 0.1,
        Span = 2,
        SpanAngle = 30,
      };
      StructuralLoadPlaneToNative.ToNative(plane1).Split('\n');

      var plane2 = new StructuralLoadPlane()
      {
        ApplicationId = "lp2",
        ElementDimension = 1,
        Tolerance = 0.1,
        Span = 1,
        SpanAngle = 0,
        StoreyRef = "TestStorey"
      };
      StructuralLoadPlaneToNative.ToNative(plane2).Split('\n');

      var loadCase1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "LcDead", Name = "Dead Load Case" };
      StructuralLoadCaseToNative.ToNative(loadCase1);

      var polylineCoords = CreateFlatRectangleCoords(0, 0, 0, 30, 5, 5);
      var loading = new StructuralVectorThree(new double[] { 0, -10, -5 });
      var load2dPanelWithoutPlane = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel1");
      Structural2DLoadPanelToNative.ToNative(load2dPanelWithoutPlane);

      var load2dPanelWithPlane1 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel2") { LoadPlaneRef = "lp1" };
      Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane1);

      var load2dPanelWithPlane2 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel3") { LoadPlaneRef = "lp2" };
      Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane2);

      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetCurrentGwa();

      //Try all the entities' GWA commands to check if the 
      Assert.IsTrue(ModelValidation(allGwa,
        new Dictionary<string, int> {
          { GsaRecord.GetKeyword<GsaAxis>(), 3 },
          { GsaRecord.GetKeyword<GsaLoadCase>(), 1 },
          { GsaRecord.GetKeyword<GsaGridPlane>(), 3 } ,
          { GsaRecord.GetKeyword<GsaGridSurface>(), 3 },
          { GsaRecord.GetKeyword<GsaLoadGridArea>(), 6 }
        },
        out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());
      Assert.Zero(((MockGSAMessenger)Initialiser.AppResources.Messenger).Messages.Count());
    }

    [Test]
    public void Structural0DSpringAndStructuralNodeToNativeTest()
    {
      var s = new Structural0DSpring()
      {
        basePoint = new SpecklePoint(100, 100, 100),
        Name = "Zee Row Dee Spring",
        ApplicationId = "0dSpring01",
        PropertyRef = "springProp"
      };

      var prop = new StructuralSpringProperty()
      {
        ApplicationId = "springProp",
        Name = "Spring Proper Tee",
        SpringType = StructuralSpringPropertyType.Friction,
        Stiffness = new StructuralVectorSix(12, 15, 20, 0, 0, 0),
        DampingRatio = 0.21
      };

      var nodes = new List<StructuralNode>()
      {
        new StructuralNode()
        {
          basePoint = new SpecklePoint(100, 100, 100),
          ApplicationId = "NodeToMatch",
          Restraint = new StructuralVectorBoolSix(new bool[] { true, true, true, false, false, false }),
          Stiffness = new StructuralVectorSix(new double[] { 11, 12, 13, 14, 15, 16}),
          Mass = 24.5,
        },
        new StructuralNode()
        {
          basePoint = new SpecklePoint(200, 200, 200),
          ApplicationId = "AlternativeNodeNotMeantToMatch"
        }
      };

      foreach (var n in nodes)
      {
        StructuralNodeToNative.ToNative(n);
      }
      StructuralSpringPropertyToNative.ToNative(prop);
      Structural0DSpringToNative.ToNative(s);

      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetCurrentGwa();

      //Try all the entities' GWA commands to check if the 
      Assert.IsTrue(ModelValidation(allGwa,
        new Dictionary<string, int> {
          { GsaRecord.GetKeyword<GsaNode>(), 2 },
          { GsaRecord.GetKeyword<GsaPropSpr>(), 1 },
          { GsaRecord.GetKeyword<GsaPropMass>(), 1 }
        },
        out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());
      Assert.Zero(((MockGSAMessenger)Initialiser.AppResources.Messenger).Messages.Count());
    }

    [Test]
    public void Structural2DLoadPanelToNativeTest()
    {
      var loadCaseAppId = "LoadCase1";
      var loadPanelAppId = "LoadPanel1";
      var loadCase = new StructuralLoadCase
      {
        ApplicationId = loadCaseAppId,
        CaseType = StructuralLoadCaseType.Dead
      };
      StructuralLoadCaseToNative.ToNative(loadCase);

      var loadPanel = new Structural2DLoadPanel
      {
        ApplicationId = loadPanelAppId,
        basePolyline = new SpecklePolyline(CreateFlatRectangleCoords(10, 10, 10, angleDegrees: 45, 20, 30)),
        Loading = new StructuralVectorThree(new double[] { 0, 0, 10000000 }),
        LoadCaseRef = "LoadCase1"
      };
      Structural2DLoadPanelToNative.ToNative(loadPanel).Split('\n');

      var LoadPanelGwa = ((IGSACache)Initialiser.AppResources.Cache).GetCurrentGwa();
      Assert.AreEqual(5, LoadPanelGwa.Count()); //should be a load case, axis, plane, surface and a load panel

      var gsaLoadCase = new GsaLoadCase() { ApplicationId = loadCaseAppId, CaseType = StructuralLoadCaseType.Dead, Index = 1 };
      var gsaAxis = new GsaAxis() { Index = 1, OriginX = 10, OriginY = 10, OriginZ = 10, XDirX = Math.Sqrt(2), XDirY = Math.Sqrt(2), XYDirX = -Math.Sqrt(2), XYDirY = Math.Sqrt(2) };
      var gsa2dLoadPanel = new GsaLoadGridArea();

      Assert.IsTrue(gsaAxis.Gwa(out var gsaAxisGwa));
      Assert.IsTrue(gsaLoadCase.Gwa(out var gsaLoadCaseGwa));
      Assert.IsTrue(gsa2dLoadPanel.Gwa(out var gsa2dLoadPanelGwa));
    }

    [Test]
    public void StructuralLoadCaseToNativeTest()
    {
      var load1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Generic, ApplicationId = "lc1", Name = "LoadCaseOne" };
      var load2 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "lc2", Name = "LoadCaseTwo" };

      StructuralLoadCaseToNative.ToNative(load1);
      StructuralLoadCaseToNative.ToNative(load2);

      var gwa = Initialiser.AppResources.Cache.GetGwa(GsaRecord.GetKeyword<GsaLoadCase>());
      Assert.AreEqual(2, gwa.Count());
      Assert.False(gwa.Any(g => string.IsNullOrEmpty(g)));

      Assert.IsTrue(ModelValidation(gwa, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaLoadCase>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      var gsaLoadCase1 = new GsaLoadCase();
      var gsaLoadCase2 = new GsaLoadCase();
      Assert.IsTrue(gsaLoadCase1.FromGwa(gwa[0]));
      Assert.IsTrue(gsaLoadCase2.FromGwa(gwa[1]));
    }
    #endregion

    #region ToSpeckle
    [TestCase(GSATargetLayer.Design)]
    [TestCase(GSATargetLayer.Analysis)]
    public void GsaLoadBeamToSpeckleTest(GSATargetLayer layer)
    {
      //Currently only UDL is supported, so only test that for now, despte the new schema containing classes for the other types

      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = layer;

      var gsaPrereqs = new List<GsaRecord>()
      {
        new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 },
        new GsaLoadCase() { Index = 1, ApplicationId = "LoadCase1", CaseType = StructuralLoadCaseType.Dead },
        new GsaLoadCase() { Index = 2, ApplicationId = "LoadCase2",  CaseType = StructuralLoadCaseType.Live },
      };

      if (layer == GSATargetLayer.Design)
      {
        gsaPrereqs.Add(CreateMembBeam(1, "mb1", "Beam One", 1, new List<int> { 1, 2 }, 3));
        gsaPrereqs.Add(CreateMembBeam(2, "mb2", "Beam Two", 1, new List<int> { 4, 5 }, 6));
      }
      else
      {
        //gsaPrereqs.Add(CreateElBeam(1, "eb1", "Beam One", 1, new List<int> { 1, 2 }, 3));
        //gsaPrereqs.Add(CreateElBeam(2, "eb2", "Beam Two", 1, new List<int> { 4, 5 }, 6));
      }

      //Each one is assumed to create just one GWA record each
      foreach (var g in gsaPrereqs)
      {
        Assert.IsTrue(g.Gwa(out var gwa, false));
        Assert.IsTrue(Initialiser.AppResources.Cache.Upsert(g.Keyword, g.Index.Value, gwa.First(), g.StreamId, g.ApplicationId, g.GwaSetCommandType));
      }

      var baseAppId1 = "LoadFromSpeckle1";
      var baseAppId2 = "LoadFromSpeckle2";

      //Testing grouping rules:
      //1. GSA-sourced (no Speckle Application ID) beam load records with same loading, load case & entities
      //2. Speckle-sourced beam load records with same base application ID, load case & entities whose loads can be combined

      var gsaLoadBeams = new List<GsaLoadBeamUdl>
      {
        //For design layer, the entity list contains MEMB indices, which are written in terms of groups ("G1" etc); for analysis, it's the EL indices
        CreateLoadBeamUdl(1, "", "", new List<int>() { 1 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        CreateLoadBeamUdl(2, "", "", new List<int>() { 1 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        //This one shouldn't be grouped with the first 2 since it has a different load case
        CreateLoadBeamUdl(3, "", "", new List<int>() { 1 }, 2, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        //This one should be grouped with the first 2 either since it has the same loading (although different entities) and the same load case
        CreateLoadBeamUdl(4, "", "", new List<int>() { 1, 2 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),

        CreateLoadBeamUdl(5, baseAppId1 + "_X", "", new List<int>() { 1, 2 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        CreateLoadBeamUdl(6, baseAppId1 + "_XX", "", new List<int>() { 1, 2 }, 1, AxisDirection6.XX, 15, LoadBeamAxisRefType.Global),
        //This one shouldn't be grouped with the previous two since, due to the axis (which is a sign of manual editing after previous Speckle reception), 
        //the loads can't be combined
        CreateLoadBeamUdl(7, baseAppId1 + "_Z", "", new List<int>() { 1, 2 }, 1, AxisDirection6.Z, -5, LoadBeamAxisRefType.Reference, 1),
        //This one shouldn't be grouped with 5 and 6 either since, although the loads can be combined and the entities are the same, the load case is different
        CreateLoadBeamUdl(8, baseAppId1 + "_XX", "", new List<int>() { 1, 2 }, 2, AxisDirection6.XX, 15, LoadBeamAxisRefType.Global),

        //This one doesn't share the same application ID as the others, so just verify it isn't grouped with the previous records even though its loading matches one of them
        CreateLoadBeamUdl(9, baseAppId2 + "_Y", "", new List<int>() { 1 }, 1, AxisDirection6.Y, -11, LoadBeamAxisRefType.Global)
      };
      Assert.AreEqual(0, gsaLoadBeams.Where(lb => lb == null).Count());

      foreach (var gsalb in gsaLoadBeams)
      {
        Assert.IsTrue(gsalb.Gwa(out var lbGwa, false));
        Initialiser.AppResources.Cache.Upsert(gsalb.Keyword, gsalb.Index.Value, lbGwa.First(), gsalb.StreamId, gsalb.ApplicationId, gsalb.GwaSetCommandType);
      }

      //Still using dummy objects for the ToSpeckle commands - any GsaLoadBeam concrete class can be used here
      Assert.NotNull(GsaLoadBeamToSpeckle.ToSpeckle(new GsaLoadBeamUdl()));

      var structural1DLoads = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DLoad>().Select(o => o.Value).Cast<Structural1DLoad>().ToList();

      Assert.AreEqual(6, structural1DLoads.Count());
    }

    [Test]
    public void GsaLoadNodeToSpeckleTest()
    {
      var baseAppId1 = "LoadFromSpeckle1";
      var baseAppId2 = "LoadFromSpeckle2";
      var loadCase1 = new GsaLoadCase() { Index = 1, CaseType = StructuralLoadCaseType.Dead };
      var loadCase2 = new GsaLoadCase() { Index = 2, CaseType = StructuralLoadCaseType.Live };
      var nodes = GenerateGsaNodes();
      var axis1 = new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var axis2 = new GsaAxis() { Index = 2, OriginX = 20, OriginY = -20, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var load1 = new GsaLoadNode() { Index = 1, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 1, AxisIndex = 1, LoadDirection = AxisDirection6.X, Value = 10 };
      var load2 = new GsaLoadNode() { Index = 2, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 1, AxisIndex = 2, LoadDirection = AxisDirection6.X, Value = 10 };
      var load3 = new GsaLoadNode() { Index = 3, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, AxisIndex = 1, LoadDirection = AxisDirection6.X, Value = 10 };
      var load4 = new GsaLoadNode() { Index = 4, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 2, AxisIndex = 2, LoadDirection = AxisDirection6.X, Value = 10 };
      var load5 = new GsaLoadNode() { Index = 5, ApplicationId = (baseAppId1 + "_XX"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = AxisDirection6.XX, Value = 12 };
      var load6 = new GsaLoadNode() { Index = 6, ApplicationId = (baseAppId1 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = AxisDirection6.YY, Value = 13 };
      var load7 = new GsaLoadNode() { Index = 7, ApplicationId = (baseAppId2 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = AxisDirection6.YY, Value = 14 };
      var load8 = new GsaLoadNode() { Index = 8, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = AxisDirection6.Z, Value = -10 };  //Test global without application ID

      var gsaRecords = new List<GsaRecord> { loadCase1, loadCase2 };
      gsaRecords.AddRange(nodes);
      gsaRecords.AddRange(new GsaRecord[] { axis1, axis2, load1, load2, load3, load4, load5, load6, load7, load8 });
      Assert.IsTrue(ExtractAndValidateGwa(gsaRecords, out var gwaCommands, out var mismatchByKw));

      Assert.IsTrue(UpsertGwaIntoCache(gwaCommands));

      //Ensure the prerequisite objects are in the send objects collection
      //Note: don't need Axis here as the GWA from the cache is used instead of GSA__ objects
      Conversions.ToSpeckle(new GSANode());

      var dummy = new GsaLoadNode();
      GsaLoadNodeToSpeckle.ToSpeckle(dummy);

      var sos = Initialiser.GsaKit.GSASenderObjects.Get<GSA0DLoad>().Select(g => g.Value).Cast<Structural0DLoad>().ToList();

      Assert.AreEqual(5, sos.Count());
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals(baseAppId1, StringComparison.InvariantCultureIgnoreCase) && o.Loading.Value.SequenceEqual(new double[] { 0, 0, 0, 12, 13, 0 })));
      Assert.AreEqual(0, sos.Count(o => string.IsNullOrEmpty(o.ApplicationId)));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals(baseAppId2)));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-1-2")));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-3-4")));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-8")));
      //TO DO: check if this expected output is actually correct since it was based on the way StructuralVectorSix.TransformOntoAxis works
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.Loading.Value.SequenceEqual(new double[] { 10, 20, 0, -50, 50, 30 })));
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.LoadCaseRef.Equals("gsa/LOAD_TITLE-1")));
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.LoadCaseRef.Equals("gsa/LOAD_TITLE-2")));
    }

    [Test]
    public void GsaNodeToSpeckleTest()
    {
      var propMassGwa = "SET\tPROP_MASS.3\t1\tMass\tNO_RGB\t34\t0\t0\t0\t0\t0\t0\tMOD\t100%\t100%\t100%";
      var propSpringGwa = "SET\tPROP_SPR.4\t1\tLSPxGeneral\tNO_RGB\tGENERAL\t0\t12\t0\t15\t0\t20\t0\t25\t0\t30\t0\t38\t0.21";
      var nodeGwas = new List<string>()
      {
        "SET\tNODE.3\t1\t\tNO_RGB\t628\t-107\t222.7\txzzz\tGLOBAL\t23\t1\t1\t1",
        "SET\tNODE.3\t2\t\tNO_RGB\t645.8\t-107\t222"
      };
      var gwaCommands = new List<string>
      {
        propSpringGwa,
        propMassGwa
      };
      gwaCommands.AddRange(nodeGwas);
      Assert.IsTrue(UpsertGwaIntoCache(gwaCommands));

      //The ToSpeckle ones are needed for all those with ApplicationId cross references as the ToSpeckle methods create the Application IDs if they
      //aren't already present in the SID of the GWA lines
      Assert.IsNotNull(GsaPropSprToSpeckle.ToSpeckle(new GsaPropSpr()));
      Assert.IsNotNull(GsaNodeToSpeckle.ToSpeckle(new GsaNode()));

      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>().Select(g => g.Value).Cast<StructuralNode>().ToList();
      var springs = Initialiser.GsaKit.GSASenderObjects.Get<GSA0DSpring>().Select(g => g.Value).Cast<Structural0DSpring>().ToList();

      Assert.AreEqual(2, nodes.Count());
      Assert.AreEqual(1, springs.Count());
    }
    #endregion

    #region other
    [TestCase(0)]
    [TestCase(30)]
    [TestCase(180)]
    public void GsaGridSurfaceAngles(double angleDegrees)
    {
      var gsaGridSurface1 = new GsaGridSurface()
      {
        Name = "Surface1",
        ApplicationId = "lgs1",
        Index = 1,
        PlaneRefType = GridPlaneAxisRefType.Global,
        Type = GridSurfaceElementsType.OneD,
        //leave entities blank, which will be treated as "all"
        Tolerance = 0.01,
        Span = GridSurfaceSpan.One,
        Angle = angleDegrees,
        Expansion = GridExpansion.PlaneCorner
      };

      Assert.IsTrue(gsaGridSurface1.Gwa(out var gwa, false));
      Assert.IsNotNull(gwa);
      Assert.Greater(gwa.Count(), 0);
      Assert.IsFalse(string.IsNullOrEmpty(gwa.First()));
      Assert.IsTrue(ModelValidation(gwa.First(), GsaRecord.GetKeyword<GsaGridSurface>(), 1, out var mismatch, visible: false));
      Assert.AreEqual(0, mismatch);
    }

    //Both ToNative and ToSpeckle
    [Test]
    public void Structural0DLoad()
    {
      //PREREQUISITES/REFERENCES - CONVERT TO GSA

      var node1 = new StructuralNode() { ApplicationId = "Node1", Name = "Node One", basePoint = new SpecklePoint(1, 2, 3) };
      var node2 = new StructuralNode() { ApplicationId = "Node2", Name = "Node Two", basePoint = new SpecklePoint(4, 5, 6) };
      var loadcase = new StructuralLoadCase() { ApplicationId = "LoadCase1", Name = "Load Case One", CaseType = StructuralLoadCaseType.Dead };
      StructuralNodeToNative.ToNative(node1);
      StructuralNodeToNative.ToNative(node2);
      //Helper.GwaToCache(Conversions.ToNative(node1), streamId1);
      //Helper.GwaToCache(Conversions.ToNative(node2), streamId1);
      StructuralLoadCaseToNative.ToNative(loadcase);

      //OBJECT UNDER TEST - CONVERT TO GSA

      var loading = new double[] { 10, 20, 30, 40, 50, 60 };
      var receivedObj = new Structural0DLoad()
      {
        ApplicationId = "Test0DLoad",
        Loading = new StructuralVectorSix(loading),
        NodeRefs = new List<string> { "Node1", "Node2" },
        LoadCaseRef = "LoadCase1"
      };
      Structural0DLoadToNative.ToNative(receivedObj);

      var cacheForTesting = ((IGSACacheForTesting)Initialiser.AppResources.Cache);
      cacheForTesting.SetStream("Node1", streamId1);
      cacheForTesting.SetStream("Node2", streamId1);
      cacheForTesting.SetStream("LoadCase1", streamId1);
      cacheForTesting.SetStream("Test0DLoad", streamId1);

      ((IGSACache)Initialiser.AppResources.Cache).Snapshot(streamId1);

      //PREREQUISITES/REFERENCES - CONVERT TO SPECKLE
      GsaNodeToSpeckle.ToSpeckle(new GsaNode());
      //Conversions.ToSpeckle(new GSANode());
      Conversions.ToSpeckle(new GSALoadCase());

      //OBJECT UNDER TEST - CONVERT TO SPECKLE

      GsaLoadNodeToSpeckle.ToSpeckle(new GsaLoadNode());
      //Conversions.ToSpeckle(new GSA0DLoad());

      var sentObjectsDict = Initialiser.GsaKit.GSASenderObjects.GetAll();
      Assert.IsTrue(sentObjectsDict.ContainsKey(typeof(GSA0DLoad)));

      var gsaLoadNodes = sentObjectsDict[typeof(GSA0DLoad)];
      var sentObjs = gsaLoadNodes.Select(o => ((IGSAContainer<Structural0DLoad>)o).Value).Cast<Structural0DLoad>().ToList();
      Assert.AreEqual(1, sentObjs.Count());
      Assert.IsTrue(sentObjs.First().Loading.Value.SequenceEqual(loading));
    }

    [TestCase(GSATargetLayer.Design)]
    [TestCase(GSATargetLayer.Analysis)]
    public void Structural1DLoad(GSATargetLayer layer)
    {
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = layer;

      var loadCase = new StructuralLoadCase()
      {
        ApplicationId = "gh/16c5d83d5f6226cc18c0a6489689fc90",
        CaseType = StructuralLoadCaseType.Live,
        Name = "Live Loads"
      };
      StructuralLoadCaseToNative.ToNative(loadCase);

      var materialSteel = new StructuralMaterialSteel()
      {
        ApplicationId = "gh/3eef066b812432a598be446180b74195"
      };
      Helper.GwaToCache(Conversions.ToNative(materialSteel), streamId1);

      var prop1d = new Structural1DProperty()
      {
        ApplicationId = "gh/d55f6475ea931e3ebfe0d81065486370",
        Profile = new SpecklePolyline(new double[] { 0, 0, 0, 500, 0, 0, 500, 500, 0, 0, 500, 0 }),
        Shape = Structural1DPropertyShape.Rectangular,
        MaterialRef = "gh/3eef066b812432a598be446180b74195",
      };
      Helper.GwaToCache(Conversions.ToNative(prop1d), streamId1);

      var elements = new List<Structural1DElement>
      {
        new Structural1DElement()
        {
          ApplicationId = "gh/b4db9f1651ca1189a64582098de85d37",
          Value = new List<double>() { 18742.85535595166, -98509.31912320339, 0, 31898.525787319068, -52834.7904308187, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        },
        new Structural1DElement()
        {
          ApplicationId = "gh/3c73c2754d2b24f42ec0bdea51133372",
          Value = new List<double>() { -5900.5943603799719, -38540.61268631692, 0, 18177.8512833416, 39670.54271647791, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        },
        new Structural1DElement()
        {
          ApplicationId = "gh/169aa416831dd4c282ec7050156037c4",
          Value = new List<double>() { -30544.044076711598, 21428.093750569555, 0, 4457.17677936413, 132175.8758637745, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        }
      };
      foreach (var e in elements)
      {
        Helper.GwaToCache(Conversions.ToNative(e), streamId1);
      }

      //Based on YEKd4q0p9 on Canada server - not sure why the loading has an application ID!
      var loading = new StructuralVectorSix(new double[] { 0, 0, -8, 0, 0, 0 }, "gh/7c2df985ad21853a345f7a85edd3b47f");
      var load = new Structural1DLoad()
      {
        ApplicationId = "gh/44d23deb343b84e0a7fc95ce37604314",
        Loading = loading,
        ElementRefs = new List<string>
        {
          "gh/b4db9f1651ca1189a64582098de85d37",
          "gh/3c73c2754d2b24f42ec0bdea51133372",
          "gh/169aa416831dd4c282ec7050156037c4"
        },
        LoadCaseRef = "gh/16c5d83d5f6226cc18c0a6489689fc90"
      };

      Structural1DLoadToNative.ToNative(load);
      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetNewGwaSetCommands();

      ((IGSACache)Initialiser.AppResources.Cache).Snapshot(streamId1);

      var entityKeyword = (layer == GSATargetLayer.Design) ? GsaRecord.GetKeyword<GsaMemb>() : GsaRecord.GetKeyword<GsaEl>();
      var loadBeamKeyword = GsaRecord.GetKeyword<GsaLoadBeam>();
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(entityKeyword, out var gwaEntities, out var _, out var _));
      Assert.AreEqual(3, gwaEntities.Count());
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(typeof(GSA1DProperty).GetGSAKeyword(), out var gwa1dProp, out var _, out var _));
      Assert.AreEqual(1, gwa1dProp.Count());
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(loadBeamKeyword, out var gwaLoadBeam, out var _, out var _));
      Assert.AreEqual(1, gwaLoadBeam.Count());

      var expectedCountByKw = new Dictionary<string, int>()
      {
        { loadBeamKeyword, 1},
        { "SECTION", 1 }, //PROP_SEC is written but SECTION is returned by GSA
        { entityKeyword, 3 }
      };
      Assert.IsTrue(ModelValidation(allGwa, expectedCountByKw, out var mismatchByKw, false));
      Assert.AreEqual(0, mismatchByKw.Keys.Count());
    }

    [Test]
    public void Structural1DPropertyExplicit()
    {
      var steel = new StructuralMaterialSteel(200000, 76923.0769, 0.3, 7850, 0.000012, 300, 440, 0.05, "steel");
      var concrete = new StructuralMaterialConcrete(29910.2016, 12462.584, 0.2, 2400, 0.00001, 12.8, 0.003, 0.02, "conc");

      var gwaSteel = steel.ToNative();
      var gwaConcrete = concrete.ToNative();
      var gwaPropNonExp = "SET\tSECTION.7:{speckle_app_id:columnProp}\t1\tNO_RGB\tColumn property\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tCONCRETE\t1\tGEO P(m) M(-0.836|-1.141) L(-3.799|3.396) L(1.71|0.992) L(0.931|-0.92) M(-1.881|1.512) L(-0.247|1.195) L(-0.431|-0.418) M(1.313|0.588) L(1.098|0.683) L(1.115|0.273) L(1.24|0.259)\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t89.99999998\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t1\t0\t0\t0\t1\t0\tNO\tNO\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON";
      Helper.GwaToCache(gwaSteel, streamId1);
      Helper.GwaToCache(gwaConcrete, streamId1);
      Helper.GwaToCache(gwaPropNonExp, streamId1);

      var propExp1 = new Structural1DPropertyExplicit() { ApplicationId = "propexp1", Name = "PropExp1", MaterialRef = "steel", Area = 11, Iyy = 21, Izz = 31, J = 41, Ky = 51, Kz = 61 };
      var propExp2 = new Structural1DPropertyExplicit() { ApplicationId = "propexp2", Name = "PropExp2", MaterialRef = "conc", Area = 12, Iyy = 22, Izz = 32, J = 42, Ky = 52, Kz = 62 };
      var propExp3 = new Structural1DPropertyExplicit() { ApplicationId = "propexp3", Name = "PropExp3", Area = 13, Iyy = 23, Izz = 33, J = 43, Ky = 53, Kz = 63 };

      Structural1DPropertyExplicitToNative.ToNative(propExp1);
      Structural1DPropertyExplicitToNative.ToNative(propExp2);
      Structural1DPropertyExplicitToNative.ToNative(propExp3);

      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetNewGwaSetCommands();
      var expectedCountByKw = new Dictionary<string, int>()
      {
        { "MAT_STEEL", 1},
        { "MAT_CONCRETE", 1 },
        { "SECTION", 4 }
      };
      Assert.IsTrue(ModelValidation(allGwa, expectedCountByKw, out var mismatchByKw, false));
      Assert.AreEqual(0, mismatchByKw.Keys.Count());

      //Check all the FromGwa commands - this includes the non-EXP one since the keyword for GSA1DPropertyExplicit is the same as for other 1D properties
      var gsaSections = SchemaConversion.Helper.GetNewFromCache<GSA1DPropertyExplicit, GsaSection>();
      Assert.AreEqual(4, gsaSections.Count());

      (new GSAMaterialConcrete()).ToSpeckle();
      (new GSAMaterialSteel()).ToSpeckle();

      var dummy = new GsaSection();
      GsaSectionToSpeckle.ToSpeckle(dummy);

      var newPropExps = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DPropertyExplicit>();

      newPropExps[0].SpeckleObject.ShouldDeepEqual(propExp1);
      newPropExps[1].SpeckleObject.ShouldDeepEqual(propExp2);
      newPropExps[2].SpeckleObject.ShouldDeepEqual(propExp3);
    }

    [Test]
    public void StructuralAssembly()
    {
      var assembly1 = new StructuralAssembly() { ApplicationId = "gh/d73615b388a8c37b6322a607a2ed5e60", Name = "C1W3S3_L5" };
      assembly1.BaseLine = new SpeckleLine(new List<double> { 41.6, 20.5, 27.25, 40.6, 20.5, 27.25 });
      assembly1.PointDistances = new List<double>() { 0.5500000000000043, 0.5500000000000043, 0.5500000000000043,
        0.5499999999999972, 0.5499999999999972, 0.5499999999999972,
        0.5, 0.5, 0.5,
        1.5007648109742667, 1.5007648109742667, 1.5007648109742667,
        0.7503825000000006, 0.7503825000000006, 0.7503825000000006,
        0.5, 0.5, 0.5};
      assembly1.OrientationPoint = new SpecklePoint(41.6, 20.5, 26.5);
      assembly1.Width = 1.5;
      assembly1.ElementRefs = new List<string>() { "A", "B", "C" };

      var assembly2 = new StructuralAssembly() { ApplicationId = "gh/7fc46bbaaa572ce40c6205d1602677f9", Name = "C1W1P1" };
      assembly2.BaseLine = new SpeckleLine(new List<double> { 40.333333499999998, 11.5, 0, 40.333333499999998, 11.5, 48 });
      assembly2.PointDistances = new List<double>() { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48 };
      assembly2.OrientationPoint = new SpecklePoint(39.6, 11.5, 48);
      assembly2.Width = 1.466666999999994;
      assembly2.ElementRefs = new List<string>() { "D", "E", "F" };

      StructuralAssemblyToNative.ToNative(assembly1);
      StructuralAssemblyToNative.ToNative(assembly2);

      var gwa = Initialiser.AppResources.Cache.GetGwa(GsaRecord.GetKeyword<GsaAssembly>());
      Assert.AreEqual(2, gwa.Count());
      Assert.False(gwa.Any(g => string.IsNullOrEmpty(g)));

      Assert.IsTrue(ModelValidation(gwa, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaAssembly>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      var gsaAssembly1 = new GsaAssembly();
      Assert.IsTrue(gsaAssembly1.FromGwa(gwa[0]));
    }
    #endregion
    #endregion

    #region data_gen_fns
    private List<GsaEl> GenerateMixedGsaEls()
    {
      var gsaElBeam = new GsaEl()
      {
        ApplicationId = "elbeam",
        Name = "Beam",
        Index = 1,
        Type = ElementType.Beam, //*
        Group = 1,
        PropertyIndex = 2,
        NodeIndices = new List<int> { 3, 4 },
        OrientationNodeIndex = 5,
        Angle = 6,
        ReleaseInclusion = ReleaseInclusion.Included,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 7 }, //*
        End1OffsetX = 8,
        End2OffsetX = 9,
        OffsetY = 10,
        OffsetZ = 11,
        ParentIndex = 1
      };

      var gsaElTri3 = new GsaEl()
      {
        ApplicationId = "eltri3",
        Name = "Triangle 3",
        Index = 2,
        Type = ElementType.Triangle3, //*
        Group = 1,
        PropertyIndex = 3,
        NodeIndices = new List<int> { 4, 5, 6 },
        OrientationNodeIndex = 7,
        Angle = 8,
        ReleaseInclusion = ReleaseInclusion.NotIncluded,  //only BEAMs have releases
        End1OffsetX = 10
      };

      return new List<GsaEl> { gsaElBeam, gsaElTri3 };
    }

    private List<GsaNode> GenerateGsaNodes()
    {
      var node1 = new GsaNode() { Index = 1, X = 10, Y = 10, Z = 0 };
      var node2 = new GsaNode() { Index = 2, X = 30, Y = -10, Z = 10 };
      return new List<GsaNode> { node1, node2 };
    }
    #endregion

    #region other_methods

    //Since the classes don't have constructors with parameters (by design, to avoid schema complexity for now), use this method instead
    private GsaLoadBeamUdl CreateLoadBeamUdl(int index, string applicationId, string name, List<int> entities, int loadCaseIndex, AxisDirection6 loadDirection,
      double load, LoadBeamAxisRefType axisRefType, int? axisIndex = null)
    {
      return new GsaLoadBeamUdl()
      {
        Index = index,
        ApplicationId = applicationId,
        Name = name,
        Entities = entities,
        LoadCaseIndex = loadCaseIndex,
        LoadDirection = loadDirection,
        Load = load,
        AxisRefType = axisRefType,
        AxisIndex = axisIndex
      };
    }

    private GsaMemb CreateMembBeam(int index, string applicationId, string name, int propIndex, List<int> nodeIndices, int orientationNodeIndex)
    {
      var gsaMemb = new GsaMemb()
      {
        ApplicationId = applicationId,
        Name = name,
        Index = index,
        Type = MemberType.Beam,
        Exposure = ExposedSurfaces.ALL,
        PropertyIndex = propIndex,
        Group = index,
        NodeIndices = nodeIndices,
        OrientationNodeIndex = orientationNodeIndex,
        IsIntersector = true,
        AnalysisType = AnalysisType.BEAM,
        RestraintEnd1 = Restraint.Fixed,
        RestraintEnd2 = Restraint.Fixed,
        EffectiveLengthType = EffectiveLengthType.Automatic,
        LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre,
        MemberHasOffsets = false
      };
      return gsaMemb;
    }

    private double[] CreateFlatRectangleCoords(double x, double y, double z, double angleDegrees, double width, double depth)
    {
      var xUnitDir = UnitVector3D.XAxis.Rotate(UnitVector3D.ZAxis, Angle.FromDegrees(angleDegrees));
      var yUnitDir = xUnitDir.Rotate(UnitVector3D.ZAxis, Angle.FromDegrees(90));

      var p1 = new Point3D(x, y, z);
      var p2 = p1 + xUnitDir.ToVector3D().ScaleBy(width);
      var p3 = p2 + yUnitDir.ToVector3D().ScaleBy(depth);
      var p4 = p1 + yUnitDir.ToVector3D().ScaleBy(depth);

      var coords = new List<double>() { p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z, p3.X, p3.Y, p3.Z, p4.X, p4.Y, p4.Z };
      return coords.ToArray();
    }

    private bool UpsertGwaIntoCache(List<string> gwaCommands)
    {
      foreach (var gwaC in gwaCommands)
      {
        GSAProxy.ParseGeneralGwa(gwaC, out var keyword, out var index, out var streamId, out var applicationId, out var gwaWithoutSet, out var gwaSetCommandType);
        if (!Initialiser.AppResources.Cache.Upsert(keyword, index.Value, gwaWithoutSet, streamId, applicationId, gwaSetCommandType.Value))
        {
          return false;
        }
      }
      return true;
    }

    private bool ExtractAndValidateGwa(IEnumerable<GsaRecord> records, out List<string> gwaLines, out Dictionary<string, int> mismatchByKw, bool visible = false)
    {
      gwaLines = new List<string>();
      var numByKw = new Dictionary<string, int>();
      foreach (var r in records)
      {
        if (r.Gwa(out var gwa, true))
        {
          foreach (var gwaL in gwa)
          {
            var pieces = gwaL.Split('\t');
            var keyword = pieces[0].Equals("SET_AT", StringComparison.InvariantCultureIgnoreCase) ? pieces[2] : pieces[1];
            keyword = keyword.Split(':').First().Split('.').First();
            if (!numByKw.ContainsKey(keyword))
            {
              numByKw.Add(keyword, 0);
            }
            numByKw[keyword]++;
          }
          gwaLines.AddRange(gwa);
        }
      }

      return ModelValidation(gwaLines, numByKw, out mismatchByKw, visible: visible);
    }

    private List<string> CollateGwaCommands(IEnumerable<GsaRecord> records)
    {
      var gwaLines = new List<string>();
      foreach (var r in records)
      {
        if (r.Gwa(out var gwa, true))
        {
          gwaLines.AddRange(gwa);
        }
      }
      return gwaLines;
    }
    #endregion

    #region model_validation_fns
    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(string gwaCommand, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(new string[] { gwaCommand }, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    private bool ModelValidation(IEnumerable<string> gwaCommands, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(gwaCommands, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(IEnumerable<string> gwaCommands, Dictionary<string, int> expectedCountByKw, out Dictionary<string, int> mismatchByKw, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      mismatchByKw = new Dictionary<string, int>();

      //Use a real proxy, not the mock one used elsewhere in tests
      var gsaProxy = new GSAProxy();
      gsaProxy.NewFile(visible);
      foreach (var gwaC in gwaCommands)
      {
        gsaProxy.SetGwa(gwaC);
      }
      gsaProxy.Sync();
      if (visible)
      {
        gsaProxy.UpdateViews();
      }
      var lines =  gsaProxy.GetGwaData(expectedCountByKw.Keys, nodesWithAppIdOnly);
      lines.ForEach(l => l.Keyword = Helper.RemoveVersionFromKeyword(l.Keyword));
      gsaProxy.Close();

      foreach (var k in expectedCountByKw.Keys)
      {
        var numFound = lines.Where(l => l.Keyword.Equals(k, StringComparison.InvariantCultureIgnoreCase)).Count();
        if (numFound != expectedCountByKw[k])
        {
          mismatchByKw.Add(k, numFound);
        }
      }

      return (mismatchByKw.Keys.Count() == 0);
    }
    #endregion
  }
}
