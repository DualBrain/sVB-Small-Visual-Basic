﻿Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports Microsoft.SmallVisualBasic.Expressions
Imports Microsoft.SmallVisualBasic.Library
Imports Microsoft.SmallVisualBasic.Statements

Namespace Microsoft.SmallVisualBasic
    Public Class VisualBasicExporter
        Private indentationLevel As Integer
        Private writer As TextWriter
        Private parser As Parser
        Private compiler As Compiler
        Private keywords As List(Of String) = New List(Of String) From {
            "addhandler",
            "addressof",
            "alias",
            "and",
            "andalso",
            "ansi",
            "as",
            "assembly",
            "auto",
            "boolean",
            "byref",
            "byte",
            "byval",
            "call",
            "case",
            "catch",
            "cbool",
            "cbyte",
            "cchar",
            "cdate",
            "cdec",
            "cdbl",
            "char",
            "cint",
            "class",
            "clng",
            "cobj",
            "const",
            "cshort",
            "csng",
            "cstr",
            "ctype",
            "date",
            "decimal",
            "declare",
            "default",
            "delegate",
            "dim",
            "directcast",
            "do",
            "double",
            "each",
            "else",
            "elseif",
            "end",
            "enum",
            "erase",
            "error",
            "event",
            "exit",
            "false",
            "finally",
            "for",
            "friend",
            "function",
            "get",
            "gettype",
            "gosub",
            "goto",
            "handles",
            "if",
            "implements",
            "imports",
            "in",
            "inherits",
            "integer",
            "interface",
            "is",
            "let",
            "lib",
            "like",
            "long",
            "loop",
            "main",
            "me",
            "mod",
            "module",
            "mustinherit",
            "mustoverride",
            "mybase",
            "myclass",
            "namespace",
            "new",
            "next",
            "not",
            "nothing",
            "notinheritable",
            "notoverridable",
            "object",
            "on",
            "option",
            "optional",
            "or",
            "orelse",
            "overloads",
            "overridable",
            "overrides",
            "paramarray",
            "preserve",
            "private",
            "property",
            "protected",
            "public",
            "raiseevent",
            "readonly",
            "redim",
            "rem",
            "removehandler",
            "resume",
            "return",
            "select",
            "set",
            "shadows",
            "shared",
            "short",
            "single",
            "static",
            "step",
            "stop",
            "string",
            "structure",
            "sub",
            "synclock",
            "then",
            "throw",
            "to",
            "true",
            "try",
            "typeof",
            "unicode",
            "until",
            "variant",
            "when",
            "while",
            "with",
            "withevents",
            "writeonly",
            "xor"
        }
        Private conflictingTypes As List(Of String) = New List(Of String) From {
            "math",
            "array"
        }

        Public Sub New(compiler As Compiler)
            If compiler Is Nothing Then
                Throw New ArgumentNullException("compiler")
            End If

            Me.compiler = compiler
            parser = compiler.Parser

            If parser.Errors.Count > 0 Then
                Throw New InvalidOperationException()
            End If
        End Sub

        Private Sub CreateProgramFile(filePath As String, parser As Parser, moduleName As String)
            Using writer As New StreamWriter(filePath)
                writer.WriteLine("Module {0}", moduleName)
                indentationLevel += 1
                EmitVariableDeclarations(parser)
                Indent()
                writer.WriteLine("Sub Main()")
                indentationLevel += 1

                For Each item In parser.ParseTree
                    EmitStatement(item)
                Next

                indentationLevel -= 1
                Indent()
                writer.WriteLine("End Sub")

                For Each item2 In parser.ParseTree
                    Dim subroutineStatement As SubroutineStatement = TryCast(item2, SubroutineStatement)

                    If subroutineStatement IsNot Nothing Then
                        EmitSubroutineStatement(subroutineStatement)
                    End If
                Next

                indentationLevel -= 1
                writer.WriteLine("End Module")
            End Using
        End Sub

        Private Sub CreateProject(projectPath As String, assemblyName As String, moduleName As String)
            Dim stringBuilder As StringBuilder = New StringBuilder(GetProjectTemplateContents())
            stringBuilder.Replace("$$(STARTUP_OBJECT)", assemblyName & "." & moduleName)
            stringBuilder.Replace("$$(ASSEMBLY_NAME)", assemblyName)
            stringBuilder.Replace("$$(SMALLBASIC_LIBRARY_PATH)", GetSmallVisualBasicLibraryPath())
            stringBuilder.Replace("$$(MODULE_FILE)", moduleName & ".vb")
            IO.File.WriteAllText(projectPath, stringBuilder.ToString(), Encoding.UTF8)
        End Sub

        Private Function ConvertExpressionToVB(expression As Expression) As String
            Dim type As Type = expression.GetType()

            If type Is GetType(BinaryExpression) Then
                Return ConvertBinaryExpressionToVB(TryCast(expression, BinaryExpression))
            End If

            If type Is GetType(ArrayExpression) Then
                Return ConvertArrayExpressionToVB(TryCast(expression, ArrayExpression))
            End If

            If type Is GetType(IdentifierExpression) Then
                Return ConvertIdentifierExpressionToVB(TryCast(expression, IdentifierExpression))
            End If

            If type Is GetType(LiteralExpression) Then
                Return ConvertLiteralExpressionToVB(TryCast(expression, LiteralExpression))
            End If

            If type Is GetType(MethodCallExpression) Then
                Return ConvertMethodCallExpressionToVB(TryCast(expression, MethodCallExpression))
            End If

            If type Is GetType(NegativeExpression) Then
                Return ConvertNegativeExpressionToVB(TryCast(expression, NegativeExpression))
            End If

            If type Is GetType(PropertyExpression) Then
                Return ConvertPropertyExpressionToVB(TryCast(expression, PropertyExpression))
            End If

            Return ""
        End Function

        Private Sub EmitStatement(statement As Statement)
            Dim type As Type = statement.GetType()

            If type Is GetType(AssignmentStatement) Then
                EmitAssignmentStatement(TryCast(statement, AssignmentStatement))
            ElseIf type Is GetType(ElseIfStatement) Then
                EmitElseIfStatement(TryCast(statement, ElseIfStatement))
            ElseIf type Is GetType(EmptyStatement) Then
                EmitEmptyStatement(TryCast(statement, EmptyStatement))
            ElseIf type Is GetType(ForStatement) Then
                EmitForStatement(TryCast(statement, ForStatement))
            ElseIf type Is GetType(GotoStatement) Then
                EmitGotoStatement(TryCast(statement, GotoStatement))
            ElseIf type Is GetType(IfStatement) Then
                EmitIfStatement(TryCast(statement, IfStatement))
            ElseIf type Is GetType(LabelStatement) Then
                EmitLabelStatement(TryCast(statement, LabelStatement))
            ElseIf type Is GetType(MethodCallStatement) Then
                EmitMethodCallStatement(TryCast(statement, MethodCallStatement))
            ElseIf type Is GetType(SubroutineCallStatement) Then
                EmitSubroutineCallStatement(TryCast(statement, SubroutineCallStatement))
            ElseIf type Is GetType(WhileStatement) Then
                EmitWhileStatement(TryCast(statement, WhileStatement))
            End If
        End Sub

        Private Sub EmitVariableDeclarations(parser As Parser)
            If parser.SymbolTable.GlobalVariables.Count = 0 Then
                Return
            End If

            Indent()
            writer.Write("Dim ")
            Dim flag = True

            For Each variable In parser.SymbolTable.GlobalVariables
                Dim text = NormalizeVariable(variable.Value.Text)

                If flag Then
                    writer.Write(text)
                    flag = False
                Else
                    writer.Write(", " & text)
                End If
            Next

            writer.WriteLine(" As Primitive")
        End Sub

        Public Function ExportToVisualBasicProject(projectName As String, targetLocation As String) As String
            Dim text = MakeSafe(projectName)
            Dim text2 = text & "Module"
            Dim text3 = Path.Combine(targetLocation, text & ".vbproj")
            CreateProject(text3, text, text2)
            Dim filePath = Path.Combine(targetLocation, text2 & ".vb")
            CreateProgramFile(filePath, parser, text2)
            Return text3
        End Function

        Private Function GetProjectTemplateContents() As String
            Dim executingAssembly As Assembly = Assembly.GetExecutingAssembly()
            Dim manifestResourceStream = executingAssembly.GetManifestResourceStream("Microsoft.SmallVisualBasic.VisualBasicProjectTemplate.vbproj")
            Return New StreamReader(manifestResourceStream).ReadToEnd()
        End Function

        Private Function GetSmallVisualBasicLibraryPath() As String
            Dim assembly = GetType(Primitive).Assembly
            Dim folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            Dim text = assembly.Location

            If text.ToUpperInvariant().StartsWith(folderPath.ToUpperInvariant()) Then
                text = "$(programfiles)\" & text.Substring(folderPath.Length)
            End If

            Return text
        End Function

        Private Sub Indent()
            writer.Write(New String(" "c, indentationLevel * 4))
        End Sub

        Private Function MakeSafe(name As String) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()

            For Each c In name

                If Char.IsLetterOrDigit(c) OrElse c = "_"c Then
                    stringBuilder.Append(c)
                Else
                    stringBuilder.Append("_"c)
                End If
            Next

            Return stringBuilder.ToString()
        End Function

        Private Function NormalizeTypeName(typeName As String) As String
            If conflictingTypes.Contains(typeName.ToLowerInvariant()) Then
                Return "Microsoft.SmallVisualBasic.Library." & typeName
            End If

            Return typeName
        End Function

        Private Function NormalizeVariable(variableName As String) As String
            If keywords.Contains(variableName.ToLowerInvariant()) Then
                Return "__" & variableName
            End If

            Return variableName
        End Function

        Private Function ConvertArrayExpressionToVB(arrayExpression As ArrayExpression) As String
            Return $"{ConvertExpressionToVB(arrayExpression.LeftHand)}({ConvertExpressionToVB(arrayExpression.Indexer)})"
        End Function

        Private Function ConvertBinaryExpressionToVB(binaryExpression As BinaryExpression) As String
            Dim text = ConvertExpressionToVB(binaryExpression.RightHandSide)
            Dim literalExpression As LiteralExpression = TryCast(binaryExpression.RightHandSide, LiteralExpression)
            Dim operatorPriority = Parser.GetOperatorPriority(binaryExpression.Operator.Type)

            If operatorPriority < 7 AndAlso literalExpression IsNot Nothing AndAlso literalExpression.Literal.Type = TokenType.StringLiteral Then
                Return $"{ConvertExpressionToVB(binaryExpression.LeftHandSide)} {binaryExpression.Operator.Text} CType({text}, Primitive)"
            End If

            Dim stringBuilder As StringBuilder = New StringBuilder()

            If operatorPriority < binaryExpression.LeftHandSide.Precedence AndAlso TypeOf binaryExpression.LeftHandSide Is BinaryExpression Then
                stringBuilder.AppendFormat("({0})", ConvertExpressionToVB(binaryExpression.LeftHandSide))
            Else
                stringBuilder.AppendFormat("{0}", ConvertExpressionToVB(binaryExpression.LeftHandSide))
            End If

            stringBuilder.AppendFormat(" {0} ", binaryExpression.Operator.Text)

            If operatorPriority < binaryExpression.RightHandSide.Precedence AndAlso TypeOf binaryExpression.RightHandSide Is BinaryExpression Then
                stringBuilder.AppendFormat("({0})", text)
            Else
                stringBuilder.AppendFormat("{0}", text)
            End If

            Return stringBuilder.ToString()
        End Function

        Private Function ConvertIdentifierExpressionToVB(identifierExpression As IdentifierExpression) As String
            Return NormalizeVariable(identifierExpression.Identifier.Text)
        End Function

        Private Function ConvertLiteralExpressionToVB(literalExpression As LiteralExpression) As String
            If Equals(literalExpression.Literal.LCaseText, """true""") Then
                Return "true"
            End If

            If Equals(literalExpression.Literal.LCaseText, """false""") Then
                Return "false"
            End If

            Return literalExpression.Literal.Text
        End Function

        Private Function ConvertMethodCallExpressionToVB(methodCallExpression As MethodCallExpression) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendFormat("{0}.{1}(", NormalizeTypeName(methodCallExpression.TypeName.Text), methodCallExpression.MethodName.Text)
            Dim flag = True

            For Each argument In methodCallExpression.Arguments

                If flag Then
                    flag = False
                    stringBuilder.Append(ConvertExpressionToVB(argument))
                Else
                    stringBuilder.AppendFormat(", {0}", ConvertExpressionToVB(argument))
                End If
            Next

            stringBuilder.Append(")")
            Return stringBuilder.ToString()
        End Function

        Private Function ConvertNegativeExpressionToVB(negativeExpression As NegativeExpression) As String
            Return "-" & ConvertExpressionToVB(negativeExpression.Expression)
        End Function

        Private Function ConvertPropertyExpressionToVB(propertyExpression As PropertyExpression) As String
            Return $"{NormalizeTypeName(propertyExpression.TypeName.Text)}.{propertyExpression.PropertyName.Text}"
        End Function

        Private Sub EmitAssignmentStatement(assignmentStatement As AssignmentStatement)
            Indent()
            Dim propertyExpression As PropertyExpression = TryCast(assignmentStatement.LeftValue, PropertyExpression)
            Dim __ As EventInfo = Nothing

            If propertyExpression IsNot Nothing Then
                Dim typeInfo = compiler.TypeInfoBag.Types(propertyExpression.TypeName.LCaseText)

                If typeInfo.Events.TryGetValue(propertyExpression.PropertyName.LCaseText, __) Then
                    writer.WriteLine("AddHandler {0}, AddressOf {1}", ConvertExpressionToVB(propertyExpression), ConvertExpressionToVB(assignmentStatement.RightValue))
                    Return
                End If
            End If

            writer.Write("{0} = {1}", ConvertExpressionToVB(assignmentStatement.LeftValue), ConvertExpressionToVB(assignmentStatement.RightValue))

            If assignmentStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", assignmentStatement.EndingComment.Text)
            End If

            writer.WriteLine()
        End Sub

        Private Sub EmitElseIfStatement(elseIfStatement As ElseIfStatement)
            Indent()
            writer.Write("ElseIf {0} Then", ConvertExpressionToVB(elseIfStatement.Condition))

            If elseIfStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", elseIfStatement.EndingComment.Text)
            End If

            writer.WriteLine()
            indentationLevel += 1

            For Each thenStatement In elseIfStatement.ThenStatements
                EmitStatement(thenStatement)
            Next

            indentationLevel -= 1
        End Sub

        Private Sub EmitEmptyStatement(emptyStatement As EmptyStatement)
            If emptyStatement.EndingComment.Type <> 0 Then
                Indent()
                writer.WriteLine(emptyStatement.EndingComment.Text)
            Else
                writer.WriteLine()
            End If
        End Sub

        Private Sub EmitForStatement(forStatement As ForStatement)
            Indent()
            writer.Write("For {0} = {1} To {2}", forStatement.Iterator.Text, ConvertExpressionToVB(forStatement.InitialValue), ConvertExpressionToVB(forStatement.FinalValue))

            If forStatement.StepToken.ParseType <> 0 Then
                writer.Write(" Step {0}", ConvertExpressionToVB(forStatement.StepValue))
            End If

            If forStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", forStatement.EndingComment.Text)
            End If

            writer.WriteLine()
            indentationLevel += 1

            For Each item In forStatement.Body
                EmitStatement(item)
            Next

            indentationLevel -= 1
            Indent()
            writer.WriteLine("Next")
        End Sub

        Private Sub EmitGotoStatement(gotoStatement As GotoStatement)
            Indent()
            writer.Write("Goto {0}", gotoStatement.Label.Text)

            If gotoStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", gotoStatement.EndingComment.Text)
            End If

            writer.WriteLine()
        End Sub

        Private Sub EmitIfStatement(ifStatement As IfStatement)
            Indent()
            writer.Write("If {0} Then", ConvertExpressionToVB(ifStatement.Condition))

            If ifStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", ifStatement.EndingComment.Text)
            End If

            writer.WriteLine()
            indentationLevel += 1

            For Each thenStatement In ifStatement.ThenStatements
                EmitStatement(thenStatement)
            Next

            indentationLevel -= 1

            For Each elseIfStatement In ifStatement.ElseIfStatements
                EmitStatement(elseIfStatement)
            Next

            If ifStatement.ElseStatements.Count > 0 Then
                Indent()
                writer.WriteLine("Else")
                indentationLevel += 1

                For Each elseStatement In ifStatement.ElseStatements
                    EmitStatement(elseStatement)
                Next

                indentationLevel -= 1
            End If

            Indent()
            writer.WriteLine("End If")
        End Sub

        Private Sub EmitLabelStatement(labelStatement As LabelStatement)
            writer.Write("{0}:", labelStatement.LabelToken.Text)

            If labelStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", labelStatement.EndingComment.Text)
            End If

            writer.WriteLine()
        End Sub

        Private Sub EmitMethodCallStatement(methodCallStatement As MethodCallStatement)
            Indent()
            writer.Write("{0}", ConvertExpressionToVB(methodCallStatement.MethodCallExpression))

            If methodCallStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", methodCallStatement.EndingComment.Text)
            End If

            writer.WriteLine()
        End Sub

        Private Sub EmitSubroutineCallStatement(subroutineCallStatement As SubroutineCallStatement)
            Indent()
            writer.Write("{0}()", NormalizeVariable(subroutineCallStatement.Name.Text))

            If subroutineCallStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", subroutineCallStatement.EndingComment.Text)
            End If

            writer.WriteLine()
        End Sub

        Private Sub EmitSubroutineStatement(subroutineStatement As SubroutineStatement)
            Indent()
            writer.Write("Sub {0}()", NormalizeVariable(subroutineStatement.Name.Text))

            If subroutineStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", subroutineStatement.EndingComment.Text)
            End If

            writer.WriteLine()
            indentationLevel += 1

            For Each item In subroutineStatement.Body
                EmitStatement(item)
            Next

            indentationLevel -= 1
            Indent()
            writer.WriteLine("End Sub")
        End Sub

        Private Sub EmitWhileStatement(whileStatement As WhileStatement)
            Indent()
            writer.Write("While {0}", ConvertExpressionToVB(whileStatement.Condition))

            If whileStatement.EndingComment.Type <> 0 Then
                writer.Write(" {0}", whileStatement.EndingComment.Text)
            End If

            writer.WriteLine()
            indentationLevel += 1

            For Each item In whileStatement.Body
                EmitStatement(item)
            Next

            indentationLevel -= 1
            Indent()
            writer.WriteLine("End While")
        End Sub

    End Class
End Namespace
