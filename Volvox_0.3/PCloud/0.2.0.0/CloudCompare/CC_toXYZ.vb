﻿Imports System.Drawing
Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports System.IO


Public Class CC_toXYZ
    Inherits GH_Component


    Public Sub New()
        MyBase.New("Convert .xyz", "Convert", "CloudCompare convert to XYZ." & vbCrLf & "http://cloudcompare.org/", "Volvox", "CloudCompare")
    End Sub

    Public Overrides ReadOnly Property ComponentGuid As Guid
        Get
            Return GuidsRelease2.CC_toXYZ
        End Get
    End Property

    Public Overrides ReadOnly Property Exposure As GH_Exposure
        Get
            Return GH_Exposure.tertiary
        End Get
    End Property

    Protected Overrides ReadOnly Property Icon As Bitmap
        Get
            Return Volvox.Common.My.Resources.Icon_Convert
        End Get
    End Property

    Protected Overrides Sub RegisterInputParams(pManager As Grasshopper.Kernel.GH_Component.GH_InputParamManager)
        pManager.AddTextParameter("File Path", "F", "File to convert with CloudCompare to XYZ format. File path can't containt any spaces, because of the CloudCompare bug.", GH_ParamAccess.item)
        pManager.AddBooleanParameter("Run", "R", "Run script", GH_ParamAccess.item, True)
    End Sub

    Protected Overrides Sub RegisterOutputParams(pManager As Grasshopper.Kernel.GH_Component.GH_OutputParamManager)
        pManager.AddTextParameter("Log", "L", "Process Log", GH_ParamAccess.list)
        pManager.AddTextParameter("File path", "F", "New file path", GH_ParamAccess.item)
    End Sub

    Dim cmdThread As Threading.Thread = Nothing
    Dim cmdOutput As New List(Of String)
    Dim finished As Boolean = False
    Dim failed As Boolean = False
    Dim threadMade As Boolean = False
    Dim expire As Action = AddressOf ExpireComponent

    Dim filepath As String = Nothing
    Dim outputPath As String = Nothing


    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)

        Dim run As Boolean

        If Not DA.GetData(0, filepath) Then Return
        If Not DA.GetData(1, run) Then Return

        'RUN
        If run Then
            finished = False
            failed = False
            cmdOutput.Clear()
            outputPath = Nothing
            Me.Message = ""

            If File.Exists(filepath) Then
                'arguments
                Dim arg As String = String.Empty
                arg += "-SILENT -O "
                arg += filepath
                arg += " -NO_TIMESTAMP -C_EXPORT_FMT ASC -EXT xyz -AUTO_SAVE OFF -MERGE_CLOUDS -SAVE_CLOUDS"

                'run thread
                If Not threadMade Then
                    cmdThread = New Threading.Thread(AddressOf RunThread)
                    cmdThread.Start(arg)
                    threadMade = True
                End If
            End If

        End If

        'RUNNING
        If Not finished Then
            If threadMade Then
                Me.Message = "Running..."
            End If
        End If

        'FINISHED
        If finished Then
            threadMade = False
            Me.Message = "Done"
        End If

        'FAILED
        If failed Then
            Me.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "CloudCompare Failed.")
            Me.Message = ""
        End If

        DA.SetDataList(0, cmdOutput)
        DA.SetData(1, outputPath)
    End Sub

    Sub RunThread(arg)
        Dim process As System.Diagnostics.Process
        Dim p As New System.Diagnostics.ProcessStartInfo("C:\Program Files\CloudCompare\CloudCompare.exe", arg)
        p.RedirectStandardError = True
        p.RedirectStandardOutput = True
        p.CreateNoWindow = True
        p.UseShellExecute = False

        process = System.Diagnostics.Process.Start(p)

        'Read cmd output
        Dim oStreamReader As System.IO.StreamReader = process.StandardOutput
        Dim out As String
        Do
            out = oStreamReader.ReadLine()

            If Not (out Is Nothing) Then
                If out.Contains("[ERROR]") Then
                    failed = True
                End If
                cmdOutput.Insert(0, out)
                Rhino.RhinoApp.MainApplicationWindow.Invoke(expire)
            End If
        Loop Until out Is Nothing


        'wait for process to finish
        process.WaitForExit()

        'filePath for output file of CloudCommpare
        Dim tmpPath As String = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) & "_MERGED.xyz")
        If File.Exists(tmpPath) Then
            outputPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) & ".xyz")
            File.Copy(tmpPath, outputPath, True)
            File.Delete(tmpPath)
        Else
            failed = True
        End If

        finished = True

        'Expire Component
        Rhino.RhinoApp.MainApplicationWindow.Invoke(expire)
        Return


    End Sub

    'EXPIRE COMPONENT
    Private Sub ExpireComponent()
        Me.ExpireSolution(True)
    End Sub
End Class