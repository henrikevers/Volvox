Public Structure Files

    Public Shared Function Libs(path As String) As List(Of String)
        Dim f As New List(Of String)

        f.Add(path & "\Licence.txt")

        f.Add(path & "\Volvox_Cloud.dll")
        f.Add(path & "\Volvox_Instr.dll")
        f.Add(path & "\Volvox.Common.dll")
        f.Add(path & "\Volvox_0.4.0.1.gha")
        f.Add(path & "\Volvox_0.4.0.2.gha")

        f.Add(path & "\Volvox_Python.py")
        f.Add(path & "\VolvoxIcon.ico")

        f.Add(path & "\Crc32C.NET.dll")
        f.Add(path & "\Crc32C.NET.xml")

        f.Add(path & "\E57LibCommon.dll")
        f.Add(path & "\E57LibReader.dll")
        f.Add(path & "\E57LibWriter.dll")

        f.Add(path & "\KDTree.Core.dll")
        f.Add(path & "\KDTree.Common.dll")

        Return f
    End Function

End Structure
