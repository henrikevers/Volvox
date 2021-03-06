﻿Imports Rhino.Geometry
Imports Grasshopper
Imports Grasshopper.Kernel.Data
Imports Volvox_Cloud
Imports Volvox_Instr
Imports Volvox.Common


Public Class Instr_VoxelSub
    Inherits Instr_BaseReporting

    Public Property VoxelSize As Double = -1

    Sub New()
        VoxelSize = -1
    End Sub

    Sub New(Size As Double)
        VoxelSize = Size
    End Sub

    Public Overrides Function Execute(ByRef PointCloud As PointCloud) As Boolean

        If VoxelSize < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance Then Return False

        Dim Subsampled As New PointCloud

        Dim dt As New DataTree(Of Integer)

        If VoxelSize <> 1 Then PointCloud.Scale(1 / Math.Abs(VoxelSize))

        Dim count As Integer = PointCloud.Count
        Dim currcount As Integer = 0

        Dim counter As Integer = 0
        Dim totc As Double = 1 / PointCloud.Count
        Dim lastpercent As Integer = 0

        For i As Integer = 0 To PointCloud.Count - 1 Step 1
            counter += 1

            If lastpercent < ((counter * totc) * 100) Then
                lastpercent = 2 * Math.Ceiling((counter * totc) * 50)
                Me.ReportPercent = lastpercent
            End If

            Dim thispt As PointCloudItem = PointCloud(i)
            Dim path1 As New GH_Path(thispt.X, thispt.Y, thispt.Z)

            If Not dt.PathExists(path1) Then
                '  CopyToCloud(PointCloud, Subsampled, i)
                dt.Add(i, path1)
            End If
        Next

        For i As Integer = 0 To dt.BranchCount - 1 Step 1
            CopyToCloud(PointCloud, Subsampled, dt.Branch(i).Item(0))
        Next

        PointCloud.Dispose()
        PointCloud = Subsampled

        If VoxelSize <> 1 Then PointCloud.Scale(VoxelSize)

        Return True
    End Function

    Public Overrides ReadOnly Property InstructionGUID As Guid
        Get
            Return GuidsRelease1.Instr_SpatialSub
        End Get
    End Property

    Public Overrides ReadOnly Property InstructionType As String
        Get
            Return "Voxel subsampling"
        End Get
    End Property

End Class
