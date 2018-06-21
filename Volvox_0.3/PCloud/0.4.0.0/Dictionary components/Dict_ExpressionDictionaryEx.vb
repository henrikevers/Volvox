Imports System.Drawing
Imports System.Text.RegularExpressions
Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports Volvox.Common
Imports Volvox_Cloud

Public Class Dict_ExpressionDictionaryEx
    Inherits GH_Component

    Sub New()
        MyBase.New("Cloud Script", "ScrData", "Evaluate values with VB.NET script and save results as user data.", "Volvox", "UserData")
    End Sub

    Public Overrides ReadOnly Property ComponentGuid As Guid
        Get
            Return GuidsRelease4.Comp_DictExpressionEx
        End Get
    End Property

    Public Overrides ReadOnly Property Exposure As GH_Exposure
        Get
            Return GH_Exposure.quinary

        End Get
    End Property

    Protected Overrides ReadOnly Property Icon As Bitmap
        Get
            Return Volvox.Common.My.Resources.Icon_ScriptCloud
        End Get
    End Property

    Protected Overrides Sub RegisterInputParams(pManager As GH_InputParamManager)
        pManager.AddParameter(New Param_Cloud, "Cloud", "C", "Cloud to operate on", GH_ParamAccess.item)
        pManager.AddTextParameter("Key", "K", "Key", GH_ParamAccess.item)
        pManager.AddTextParameter("Script", "S", "Expression to evaluate", GH_ParamAccess.item)
    End Sub

    Protected Overrides Sub RegisterOutputParams(pManager As GH_OutputParamManager)
        pManager.AddParameter(New Param_Cloud, "Cloud", "C", "Modified cloud", GH_ParamAccess.item)
        pManager.AddTextParameter("Script", "S", "Script as compiled", GH_ParamAccess.item)
    End Sub

    Dim ScrSolver As Math_EvalProvider = Nothing
    Dim GlobalCloud As PointCloud = Nothing
    Dim ProcCount As Integer = Environment.ProcessorCount
    Dim Results() As Double = Nothing
    Dim PrevScript As String = String.Empty
    Dim result As New List(Of String)

    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)

        Dim strex As String = Nothing
        Dim strdc As String = Nothing

        Dim GhCloud As GH_Cloud = Nothing 'To maintain scanner Position
        If Not DA.GetData(0, GhCloud) Then Return

        Dim newGhCloud As GH_Cloud = GhCloud.Duplicate 'To maintain scanner Position
        Dim pc As PointCloud = newGhCloud.Value 'To maintain scanner Position

        GlobalCloud = pc
        ReDim Results(pc.Count - 1)

        If Not DA.GetData(1, strdc) Then Return
        If Not DA.GetData(2, strex) Then Return

        If strdc.Length < 1 Then Return
        If strex.Length < 1 Then Return

        If strex <> PrevScript Then
            result.Clear()
            ScrSolver = Nothing
            ScrSolver = New Math_EvalProvider
            result = ScrSolver.CompileEx(strex)
            PrevScript = strex
        End If

        If result.Count > 1 Then
            For Each r As String In result
                Me.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, r)
            Next
        Else
            Dim ThreadList As New List(Of Threading.Thread)

            For i As Integer = 0 To ProcCount - 1 Step 1
                Dim nt As New Threading.Thread(AddressOf EvalThread)
                nt.IsBackground = True
                ThreadList.Add(nt)
            Next

            For i As Integer = 0 To ProcCount - 1 Step 1
                ThreadList(i).Start(i)
            Next

            For Each t As Threading.Thread In ThreadList
                t.Join()
            Next

            ThreadList.Clear()
            GlobalCloud.UserDictionary.Set(strdc, Results)
            newGhCloud.Value = GlobalCloud 'To maintain scanner Position
            DA.SetData(0, newGhCloud)
            Results = Nothing
        End If

        DA.SetDataList(1, result)

    End Sub

    Sub EvalThread(MyIndex As Integer)

        Dim i0 As Integer = MyIndex * Math.Ceiling(GlobalCloud.Count / ProcCount)
        Dim i1 As Integer = Math.Min((MyIndex + 1) * Math.Ceiling(GlobalCloud.Count / ProcCount) - 1, GlobalCloud.Count - 1)

        Dim totc As Integer = GlobalCloud.Count

        For i As Integer = i0 To i1 Step 1
            Results(i) = ScrSolver.EvaluateExpression(GlobalCloud.Item(i))
        Next

    End Sub



End Class
