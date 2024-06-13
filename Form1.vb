Imports System.IO
Imports OxyPlot
Imports OxyPlot.Series
Imports OxyPlot.WindowsForms

Public Class Form1
    Private Const sampleRate As Integer = 22050
    Private flowLayoutPanel As FlowLayoutPanel
    Private plotView As PlotView
    Private splitContainer As SplitContainer

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize SplitContainer
        splitContainer = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 20,
            .Padding = New Padding(10)
        }

        ' Initialize FlowLayoutPanel and PlotView
        flowLayoutPanel = New FlowLayoutPanel With {
            .Dock = DockStyle.Left,
            .AutoScroll = True
        }
        plotView = New PlotView With {
            .Dock = DockStyle.Fill
        }

        ' Add FlowLayoutPanel to SplitContainer Panel1
        splitContainer.Panel1.Controls.Add(flowLayoutPanel)
        ' Add PlotView to SplitContainer Panel2
        splitContainer.Panel2.Controls.Add(plotView)

        ' Add SplitContainer to the Form
        Me.Controls.Add(splitContainer)

        ' Get the current directory
        Dim currentDirectory As String = My.Computer.FileSystem.CurrentDirectory
        ' Navigate three directories up
        Dim folderPath As String = Path.GetFullPath(Path.Combine(currentDirectory, "..\..\..", "Vowel_recordings"))

        ' Check if the folder exists
        If Not Directory.Exists(folderPath) Then
            MessageBox.Show("Folder does not exist: " & folderPath)
            Exit Sub
        End If

        ' Get the text files in the folder
        Dim textFiles() As String = Directory.GetFiles(folderPath, "*.txt")

        ' Create a button for each text file
        For Each filePath As String In textFiles
            Dim fileName As String = Path.GetFileName(filePath)
            Dim button As New Button With {
                .Text = fileName,
                .Tag = filePath,
                .Width = 180,
                .Height = 30
            }
            AddHandler button.Click, AddressOf FileButton_Click
            flowLayoutPanel.Controls.Add(button)
        Next
    End Sub

    Private Sub FileButton_Click(sender As Object, e As EventArgs)
        Dim button As Button = CType(sender, Button)
        Dim filePath As String = CType(button.Tag, String)

        ' Read all lines from the file and convert to Double
        Dim lines() As String = File.ReadAllLines(filePath)
        Dim samples(lines.Length - 1) As Double
        For i As Integer = 0 To lines.Length - 1
            samples(i) = Convert.ToDouble(lines(i))
        Next

        ' Perform FFT
        Dim fftResults As Complex() = FFT(samples)

        ' Compute magnitudes
        Dim magnitudes(fftResults.Length - 1) As Double
        For i As Integer = 0 To fftResults.Length - 1
            magnitudes(i) = fftResults(i).Magnitude()
        Next

        ' Prepare data for plotting
        Dim frequencies(magnitudes.Length - 1) As Double
        For i As Integer = 0 To magnitudes.Length - 1
            frequencies(i) = i * sampleRate / magnitudes.Length
        Next

        ' Plot the frequency domain
        Dim plotModel As New PlotModel With {.Title = "Frequency Domain - " & button.Text}
        Dim lineSeries As New LineSeries
        For i As Integer = 0 To magnitudes.Length - 1
            lineSeries.Points.Add(New DataPoint(frequencies(i), magnitudes(i)))
        Next
        plotModel.Series.Add(lineSeries)
        plotView.Model = plotModel
    End Sub

    Private Function FFT(samples() As Double) As Complex()
        Dim n As Integer = samples.Length
        Dim complexSamples(n - 1) As Complex
        For i As Integer = 0 To n - 1
            complexSamples(i) = New Complex(samples(i), 0)
        Next
        FourierTransform.FFT(complexSamples, FourierTransform.Direction.Forward)
        Return complexSamples
    End Function
End Class


Public Class FourierTransform
    Public Enum Direction
        Forward = 1
        Backward = -1
    End Enum

    Public Shared Sub FFT(samples() As Complex, direction As Direction)
        Dim n As Integer = samples.Length
        If n <= 1 Then Return

        Dim halfN As Integer = n \ 2
        Dim even(halfN - 1) As Complex
        Dim odd(halfN - 1) As Complex

        For i As Integer = 0 To halfN - 1
            even(i) = samples(2 * i)
            odd(i) = samples(2 * i + 1)
        Next

        FFT(even, direction)
        FFT(odd, direction)

        For k As Integer = 0 To halfN - 1
            Dim t As Complex = Complex.Exp(New Complex(0, -2 * Math.PI * k / n * CInt(direction))) * odd(k)
            samples(k) = even(k) + t
            samples(k + halfN) = even(k) - t
        Next
    End Sub
End Class

Public Class Complex
    Public Property Real As Double
    Public Property Imaginary As Double

    Public Sub New(real As Double, imaginary As Double)
        Me.Real = real
        Me.Imaginary = imaginary
    End Sub

    Public Function Magnitude() As Double
        Return Math.Sqrt(Real * Real + Imaginary * Imaginary)
    End Function

    Public Shared Operator +(c1 As Complex, c2 As Complex) As Complex
        Return New Complex(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary)
    End Operator

    Public Shared Operator -(c1 As Complex, c2 As Complex) As Complex
        Return New Complex(c1.Real - c2.Real, c1.Imaginary - c2.Imaginary)
    End Operator

    Public Shared Operator *(c1 As Complex, c2 As Complex) As Complex
        Return New Complex(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary, c1.Real * c2.Imaginary + c1.Imaginary * c2.Real)
    End Operator

    Public Shared Function Exp(c As Complex) As Complex
        Dim expReal As Double = Math.Exp(c.Real)
        Return New Complex(expReal * Math.Cos(c.Imaginary), expReal * Math.Sin(c.Imaginary))
    End Function
End Class
