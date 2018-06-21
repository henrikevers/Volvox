
Imports System.Drawing
Imports Grasshopper.GUI
Imports Grasshopper.GUI.Canvas

Public Class Cloud_EngineAtt_OBSOLETE
    Inherits Grasshopper.Kernel.Attributes.GH_ComponentAttributes

    Public Sub New(ByVal owner As Cloud_Engine_OBSOLETE)
        MyBase.New(owner)
    End Sub

    Public Overrides Function RespondToMouseDoubleClick(sender As GH_Canvas, e As GH_CanvasMouseEvent) As GH_ObjectResponse
        If Me.ContentBox.Contains(e.CanvasLocation) Then
            Return GH_ObjectResponse.Handled
        End If

        Return GH_ObjectResponse.Ignore
    End Function

End Class
