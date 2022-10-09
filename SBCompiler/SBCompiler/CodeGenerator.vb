﻿Imports System.Reflection
Imports System.Reflection.Emit
Imports Microsoft.SmallVisualBasic.Library
Imports Microsoft.SmallVisualBasic.Library.Internal

Namespace Microsoft.SmallVisualBasic
    Public Class CodeGenerator
        Private _outputName As String
        Private _directory As String
        Private _parsers As List(Of Parser)
        Private _entryPoint As MethodInfo
        Private _currentScope As CodeGenScope
        Private _typeInfoBag As TypeInfoBag

        Public Sub New(parsers As List(Of Parser), typeInfoBag As TypeInfoBag, outputName As String, directory As String)
            If parsers Is Nothing Then
                Throw New ArgumentNullException(NameOf(parsers))
            End If

            If typeInfoBag Is Nothing Then
                Throw New ArgumentNullException(NameOf(typeInfoBag))
            End If

            _parsers = parsers
            _typeInfoBag = typeInfoBag
            _outputName = outputName
            _directory = directory
        End Sub

        Public Shared IgnoreVarErrors As Boolean

        Public Shared Sub LowerAndEmit(code As String, scope As CodeGenScope, Subroutine As Statements.SubroutineStatement, lineOffset As Integer)
            IgnoreVarErrors = True
            Dim tempRoutine = Statements.SubroutineStatement.Current
            Statements.SubroutineStatement.Current = Subroutine
            Dim _parser = Parser.Parse(code, scope.SymbolTable, lineOffset)

            For Each item In _parser.ParseTree
                item.PrepareForEmit(scope)
            Next


            For Each item In _parser.ParseTree
                item.EmitIL(scope)
            Next

            IgnoreVarErrors = False
            Statements.SubroutineStatement.Current = tempRoutine

        End Sub

        Private Sub AddGlobalTypeToList(type As Type)
            Dim typeInfo As New TypeInfo
            typeInfo.Type = type

            Dim methods = type.GetMethods(BindingFlags.Public Or BindingFlags.Static)
            For Each method In methods
                If Not method.IsSpecialName Then
                    Dim name = method.Name.ToLower()
                    If name <> "initialize" Then
                        typeInfo.Methods.Add(name, method)
                    End If
                End If
            Next

            Dim props = type.GetProperties(BindingFlags.Public Or BindingFlags.Static)
            For Each prop In props
                Dim name = prop.Name.ToLower()
                typeInfo.Properties.Add(name, prop)
            Next

            If typeInfo.Methods.Count > 0 OrElse typeInfo.Properties.Count > 0 Then
                _typeInfoBag.Types("global") = typeInfo
            End If
        End Sub


        Public Sub GenerateExecutable(Optional forGlobalHelp As Boolean = False)
            Dim assemblyName As New AssemblyName(_outputName)
            Dim assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Save,
                _directory
            )

            Dim moduleBuilder = assemblyBuilder.DefineDynamicModule(_outputName & ".exe", emitSymbolInfo:=True)

            Dim mainFormInit As MethodInfo = Nothing
            Dim globalInit As MethodInfo = Nothing
            Dim formInit As MethodInfo = Nothing

            For Each parser In _parsers
                formInit = EmitModule(parser, moduleBuilder, forGlobalHelp)
                If parser.IsMainForm Then
                    mainFormInit = formInit
                ElseIf parser.IsGlobal Then
                    globalInit = formInit
                    AddGlobalTypeToList(formInit.DeclaringType)
                End If
            Next

            EmitMain(globalInit, If(mainFormInit, formInit), moduleBuilder)

            assemblyBuilder.SetEntryPoint(_entryPoint, PEFileKinds.WindowApplication)
            If Not forGlobalHelp Then assemblyBuilder.Save(_outputName & ".exe")
        End Sub

        Dim globalScope As CodeGenScope

        Private Function EmitModule(
                          parser As Parser,
                          moduleBuilder As ModuleBuilder,
                          Optional forGlobalHelp As Boolean = False
                     ) As MethodInfo

            Dim typeBuilder = moduleBuilder.DefineType(parser.ClassName, TypeAttributes.Sealed)
            Dim methodBuilder = typeBuilder.DefineMethod("Initialize", MethodAttributes.Static Or MethodAttributes.Public)
            Dim iLGenerator = methodBuilder.GetILGenerator()

            _currentScope = New CodeGenScope With {
                .ILGenerator = iLGenerator,
                .MethodBuilder = methodBuilder,
                .TypeBuilder = typeBuilder,
                .SymbolTable = parser.SymbolTable,
                .TypeInfoBag = _typeInfoBag,
                .GlobalScope = globalScope,
                .ForGlobalHelp = forGlobalHelp
            }

            If parser.IsGlobal Then
                globalScope = _currentScope
                _currentScope.GlobalScope = globalScope
            End If

            BuildFields(typeBuilder, parser.SymbolTable, parser.IsGlobal)
            EmitIL(parser.ParseTree)
            iLGenerator.Emit(OpCodes.Ret)
            typeBuilder.CreateType()
            Return methodBuilder
        End Function

        Private Function EmitMain(globalInit As MethodInfo, mainFormInit As MethodInfo, moduleBuilder As ModuleBuilder) As Boolean
            Dim typeBuilder = moduleBuilder.DefineType("_SmallVisualBasic_Program", TypeAttributes.Sealed)
            _entryPoint = typeBuilder.DefineMethod("_Main", MethodAttributes.Static)
            Dim methodBuilder = CType(_entryPoint, MethodBuilder)
            Dim iLGenerator = methodBuilder.GetILGenerator()
            Dim beginProgram = GetType(SmallBasicApplication).GetMethod(
                "BeginProgram",
                BindingFlags.Static Or BindingFlags.Public
            )
            iLGenerator.EmitCall(OpCodes.Call, beginProgram, Nothing)

            If globalInit IsNot Nothing Then
                iLGenerator.EmitCall(OpCodes.Call, globalInit, Nothing)
            End If
            iLGenerator.EmitCall(OpCodes.Call, mainFormInit, Nothing)

            Dim pauseIfVisible = GetType(TextWindow).GetMethod(
                "PauseIfVisible",
                BindingFlags.Static Or BindingFlags.Public
             )
            iLGenerator.EmitCall(OpCodes.Call, pauseIfVisible, Nothing)

            iLGenerator.Emit(OpCodes.Ret)
            typeBuilder.CreateType()
            Return True
        End Function

        Private Sub BuildFields(
                        typeBuilder As TypeBuilder,
                        symbolTable As SymbolTable,
                        isGlobal As Boolean
                    )

            For Each var In symbolTable.GlobalVariables
                Dim fieldBuilder = typeBuilder.DefineField(
                        "_" & var.Value.LCaseText,
                        GetType(Primitive),
                        FieldAttributes.Private Or FieldAttributes.Static
                )
                _currentScope.Fields.Add(var.Key, fieldBuilder)

                If isGlobal Then ' Define a public property for the field
                    Dim propName = var.Value.Text
                    Dim propBuilder = typeBuilder.DefineProperty(
                            propName,
                            PropertyAttributes.None,
                            GetType(Primitive),
                            Nothing
                    )

                    Dim attr = MethodAttributes.Public Or
                                      MethodAttributes.Static Or
                                      MethodAttributes.SpecialName Or
                                      MethodAttributes.HideBySig

                    Dim getProp = typeBuilder.DefineMethod(
                            $"get_{propName}",
                            attr,
                            GetType(Primitive),
                            Type.EmptyTypes
                    )

                    Dim getIL = getProp.GetILGenerator()
                    getIL.Emit(OpCodes.Ldsfld, fieldBuilder)
                    getIL.Emit(OpCodes.Ret)

                    Dim setProp = typeBuilder.DefineMethod(
                            $"set_{propName}",
                            attr,
                            Nothing,
                            New Type() {GetType(Primitive)}
                    )

                    Dim setIL = setProp.GetILGenerator()
                    setIL.Emit(OpCodes.Ldarg_0)
                    setIL.Emit(OpCodes.Stsfld, fieldBuilder)
                    setIL.Emit(OpCodes.Ret)

                    propBuilder.SetGetMethod(getProp)
                    propBuilder.SetSetMethod(setProp)

                    Dim returntype = _currentScope.SymbolTable.GetInferedType(var.Value)
                    If returntype <> VariableType.Any Then
                        Dim ctorParams = New Type() {GetType(VariableType)}
                        Dim ctorInfo = GetType(WinForms.ReturnValueTypeAttribute).GetConstructor(ctorParams)
                        propBuilder.SetCustomAttribute(
                            New CustomAttributeBuilder(
                                    ctorInfo,
                                    New Object() {returntype}
                            )
                        )
                    End If

                End If
            Next
        End Sub

        Private Sub EmitIL(parseTree As List(Of Statements.Statement), Optional prepareOnly As Boolean = False)
            For Each item In parseTree
                item.PrepareForEmit(_currentScope)
            Next

            If prepareOnly Then Return

            For Each item In parseTree
                item.EmitIL(_currentScope)
            Next
        End Sub

    End Class
End Namespace
