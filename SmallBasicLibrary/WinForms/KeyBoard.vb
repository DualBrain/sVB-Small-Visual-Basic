﻿Imports System.Windows
Imports System.Windows.Input
Imports Microsoft.SmallVisualBasic.Library
Imports App = Microsoft.SmallVisualBasic.Library.Internal.SmallBasicApplication

Namespace WinForms
    ''' <summary>
    ''' Contains info about the state of the keyboard keys.
    ''' </summary>
    <SmallVisualBasicType>
    Public NotInheritable Class Keyboard

        Shared Sub New()
            App.Invoke(
                Sub() EventManager.RegisterClassHandler(
                        GetType(Window),
                        UIElement.PreviewKeyDownEvent,
                        New KeyEventHandler(AddressOf KeyDown))
            )

            App.Invoke(
                Sub() EventManager.RegisterClassHandler(
                        GetType(Window),
                        UIElement.PreviewTextInputEvent,
                        New TextCompositionEventHandler(AddressOf TextInput))
            )
        End Sub

        Private Shared Sub TextInput(sender As Object, e As TextCompositionEventArgs)
            _lastTextInput = e.Text
        End Sub

        Private Shared Sub KeyDown(sender As Object, e As KeyEventArgs)
            _LastKey = e.Key
        End Sub

        ''' <summary>
        ''' Returns True if the Alt key is pressed
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property AltPressed As Primitive
            Get
                App.Invoke(
                      Sub()
                          AltPressed = (Input.Keyboard.Modifiers And ModifierKeys.Alt) > 0
                      End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Cntrl key is pressed
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property CtrlPressed As Primitive
            Get
                App.Invoke(
                     Sub()
                         CtrlPressed = (Input.Keyboard.Modifiers And ModifierKeys.Control) > 0
                     End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Shift key is pressed
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property ShiftPressed As Primitive
            Get
                App.Invoke(
                       Sub()
                           ShiftPressed = (Input.Keyboard.Modifiers And ModifierKeys.Shift) > 0
                       End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Win key is pressed
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property WinPressed As Primitive
            Get
                App.Invoke(
                    Sub()
                        WinPressed = (Input.Keyboard.Modifiers And ModifierKeys.Windows) > 0
                    End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Caps Lock key is on.
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property CapsLockOn As Primitive
            Get
                App.Invoke(
                     Sub()
                         CapsLockOn = Input.Keyboard.IsKeyToggled(Key.CapsLock)
                     End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Insert key is on.
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property InsertOn As Primitive
            Get
                App.Invoke(
                       Sub()
                           InsertOn = Input.Keyboard.IsKeyToggled(Key.Insert)
                       End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Scroll Lock key is on.
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property ScrollOn As Primitive
            Get
                App.Invoke(
                     Sub()
                         ScrollOn = Input.Keyboard.IsKeyToggled(Key.Scroll)
                     End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the Num Lock key is on.
        ''' </summary>
        <ReturnValueType(VariableType.Boolean)>
        Public Shared ReadOnly Property NumLockOn As Primitive
            Get
                App.Invoke(
                     Sub()
                         NumLockOn = Input.Keyboard.IsKeyToggled(Key.NumLock)
                     End Sub)
            End Get
        End Property

        ''' <summary>
        ''' Returns the last Key pressed on the keyboard. 
        ''' Use The Keys enum members to check they key.
        ''' Example: If Keyboard.LastKey = Keys.A Then
        ''' </summary>
        <ReturnValueType(VariableType.Key)>
        Public Shared ReadOnly Property LastKey As Primitive

        Friend Shared _lastTextInput As Primitive

        ''' <summary>
        ''' returns the last text that was about to be written to the TextBox
        ''' </summary>
        <ReturnValueType(VariableType.String)>
        Public Shared ReadOnly Property LastTextInput As Primitive
            Get
                Return _lastTextInput
            End Get
        End Property

        ''' <summary>
        ''' Returns the name of the last key pressed on the keyboard.
        ''' </summary>
        <ReturnValueType(VariableType.String)>
        Public Shared ReadOnly Property LastKeyName As Primitive
            Get
                Return [Enum].GetName(GetType(Input.Key), CInt(LastKey))
            End Get
        End Property
    End Class
End Namespace