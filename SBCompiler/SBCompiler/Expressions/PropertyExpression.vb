﻿Imports System.Reflection
Imports System.Reflection.Emit
Imports Microsoft.SmallVisualBasic.Completion
Imports Microsoft.SmallVisualBasic.Statements

Namespace Microsoft.SmallVisualBasic.Expressions
    <Serializable>
    Public Class PropertyExpression
        Inherits Expression

        Public Property TypeName As Token
        Public Property PropertyName As Token

        Public IsDynamic As Boolean
        Public isSet As Boolean
        Public IsEvent As Boolean

        Public ReadOnly Property DynamicKey As String
            Get
                Dim dynTypeName = CompletionHelper.TrimData(TypeName.Text)
                Return $"dynprop.{dynTypeName}.{PropertyName.LCaseText}"
            End Get
        End Property


        Public Overrides Sub AddSymbols(symbolTable As SymbolTable)
            MyBase.AddSymbols(symbolTable)
            _TypeName.Parent = Me.Parent
            _PropertyName.Parent = Me.Parent

            Dim name = TypeName.LCaseText
            _TypeName.SymbolType = Completion.CompletionItemType.GlobalVariable
            symbolTable.AddIdentifier(_TypeName)

            If IsDynamic OrElse name.StartsWith("data") Or name.EndsWith("data") Then
                symbolTable.AddDynamic(Me)
            End If

            ' IsDynamic can change after calling AddDynamic
            If IsDynamic Then
                _PropertyName.SymbolType = CompletionItemType.DynamicPropertyName
            Else
                _PropertyName.SymbolType = CompletionItemType.PropertyName
                symbolTable.FixNames(_TypeName, _PropertyName, False)
            End If

            symbolTable.AddIdentifier(_PropertyName)
        End Sub

        Private Shared dynCounter As Integer = 0

        Public Overrides Sub EmitIL(scope As CodeGenScope)
            If scope.ForGlobalHelp Then
                LiteralExpression.Zero.EmitIL(scope)
                Return
            End If

            If IsDynamic Then
                dynCounter += 1
                Dim code = $"_sVB_dynamic_Data_{dynCounter}={TypeName.Text}[""{PropertyName.Text}""]"
                Dim subroutine = SubroutineStatement.GetSubroutine(Me)
                If subroutine Is Nothing Then subroutine = SubroutineStatement.Current
                ArrayExpression.ParseAndEmit(code, subroutine, scope, StartToken.Line).EmitIL(scope)
            Else
                Dim typeInfo = scope.TypeInfoBag.Types(TypeName.LCaseText)
                Dim propertyInfo = typeInfo.Properties(PropertyName.LCaseText)
                Dim getMethod = propertyInfo.GetGetMethod()
                scope.ILGenerator.EmitCall(OpCodes.Call, getMethod, Nothing)
            End If
        End Sub

        Public Overrides Function ToString() As String
            Return $"{TypeName.Text}{If(IsDynamic, "!", ".")}{PropertyName.Text}"
        End Function

        Public Overrides Function InferType(symbolTable As SymbolTable) As VariableType
            If IsDynamic Then
                Return symbolTable.GetInferedType(DynamicKey)
            Else
                Return symbolTable.GetReturnValueType(_TypeName, _PropertyName, False)
            End If
        End Function
    End Class
End Namespace
