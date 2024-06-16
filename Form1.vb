Imports System.IO
Imports OxyPlot
Imports OxyPlot.Series
Imports OxyPlot.WindowsForms

Public Class Form1
    Private Const sampleRate As Integer = 22050
    Private flowLayoutPanel As FlowLayoutPanel
    Private plotView As PlotView
    Private splitContainer As SplitContainer
    Private toggleButton As Button
    Private showNyquist As Boolean = True
    Private selectedFilePath As String = ""
    Private button1 As Button

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Maximize the form to start in full screen mode
        Me.WindowState = FormWindowState.Maximized

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

        ' Initialize Toggle Button
        toggleButton = New Button With {
            .Text = "Show Full Spectrum",
            .Dock = DockStyle.Top,
            .Height = 30
        }
        AddHandler toggleButton.Click, AddressOf ToggleButton_Click

        ' Initialize Button1
        button1 = New Button With {
            .Text = "Change to DFT",
            .Dock = DockStyle.Top,
            .Height = 30
        }
        AddHandler button1.Click, AddressOf Button1_Click

        ' Add FlowLayoutPanel, Toggle Button, and Button1 to SplitContainer Panel1
        splitContainer.Panel1.Controls.Add(flowLayoutPanel)
        splitContainer.Panel1.Controls.Add(toggleButton)
        splitContainer.Panel1.Controls.Add(button1)

        ' Add PlotView to SplitContainer Panel2
        splitContainer.Panel2.Controls.Add(plotView)

        ' Add SplitContainer to the Form
        Me.Controls.Add(splitContainer)

        ' Get the current directory
        Dim currentDirectory As String = My.Computer.FileSystem.CurrentDirectory
        ' Navigate three directories up
        Dim folderPath As String = Path.GetFullPath(Path.Combine(currentDirectory, "Vowel_recordings"))

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

    Private Sub ToggleButton_Click(sender As Object, e As EventArgs)
        showNyquist = Not showNyquist
        If showNyquist Then
            toggleButton.Text = "Show Full Spectrum"
        Else
            toggleButton.Text = "Show Up to Nyquist"
        End If
        ' Refresh the plot
        RefreshPlot()
    End Sub

    Private Sub FileButton_Click(sender As Object, e As EventArgs)
        ' Update the selected file and refresh the plot
        selectedFilePath = CType(sender, Button).Tag.ToString()
        RefreshPlot()
    End Sub

    Private Sub RefreshPlot()
        If String.IsNullOrEmpty(selectedFilePath) Then
            Return
        End If

        Dim filePath As String = selectedFilePath

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

        ' Determine the range to display
        Dim displayLength As Integer
        If showNyquist Then
            displayLength = magnitudes.Length \ 2
        Else
            displayLength = magnitudes.Length
        End If
        Console.WriteLine("Length of samples array: " & samples.Length)
        Console.WriteLine("Length of magnitudes array: " & magnitudes.Length)

        ' Prepare data for plotting
        Dim frequencies(displayLength - 1) As Double
        For i As Integer = 0 To displayLength - 1
            frequencies(i) = i * sampleRate / magnitudes.Length
        Next
        Console.WriteLine("final freq: " & frequencies(displayLength - 1))
        ' Plot the frequency domain
        Dim plotModel As New PlotModel With {.Title = "FFT Frequency Domain - " & Path.GetFileName(filePath)}
        Dim lineSeries As New LineSeries
        For i As Integer = 0 To displayLength - 1
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

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        ' Open Form2 and close Form1
        Dim form2 As New Form2()
        form2.Show()
        Me.Hide()
    End Sub
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

    Public Shared ReadOnly Property Zero As Complex
        Get
            Return New Complex(0, 0)
        End Get
    End Property
End Class
