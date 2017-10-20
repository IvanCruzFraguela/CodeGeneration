using IvanCruz.Util;
using IvanCruz.CodeGeneration.SourceGeneration;
using System;
using System.Collections.Generic;
using System.IO;

namespace IvanCruz.CodeGeneration.CSharpGeneration {
	public class CSharpProject {
		public CDirectory RootDirectory = null;
		public string Name { get; set; }

		public CSharpProject(string name, string directoryName) {
			this.Name = name;
			this.RootDirectory = new CDirectory(null, directoryName);
		}
		public CDirectory AddChildDirectory(string directoryName) {
			return AddChildDirectory(this.ActualDirectory, directoryName);
		}
		public CDirectory AddChildDirectory(CDirectory baseDirectory, string directoryName) {
			return this.ActualDirectory = baseDirectory.AddDirectory(directoryName);
		}
		public CSharpFile AddCSharpFile(string fileName, string namespaceName) {
			return AddCSharpFile(fileName, new CUsing(namespaceName));
		}
		public CSharpFile AddCSharpFile(string fileName, CUsing namespaceInFile) {
			CSharpFile result = new CSharpFile(fileName, namespaceInFile);
			this.ActualDirectory.AddElement(result);
			this.ActualCSharpFile = result;
			return result;
		}
		public CClass AddClass(string name, bool isStatic = false, bool isPartial = false) {
			CClass result = this.ActualCSharpFile.AddClass(name, isStatic, isPartial);
			this.ActualClass = result;
			return result;
		}
		private CDirectory _ActualDirectory;
		public CDirectory ActualDirectory {
			get {
				if (this._ActualDirectory == null) {
					return this.RootDirectory;
				}
				return this._ActualDirectory;
			}
			set {
				_ActualDirectory = value;
			}
		}
		private CSharpFile _ActualCSharpFile;
		private CSharpFile ActualCSharpFile {
			get {
				return this._ActualCSharpFile;
			}
			set {
				this._ActualCSharpFile = value;
			}
		}

		public void Generate(TextWriter mensajes) {
			this.RootDirectory.Generate(mensajes);
		}

		private CClass _ActualClass;
		public CClass ActualClass {
			get {
				return this._ActualClass;
			}
			set {
				this._ActualClass = value;
			}
		}

		internal static void WriteVisibility(CSourceWriter sw, CSharpVisibility cSharpVisibility) {
			switch (cSharpVisibility) {
				case CSharpVisibility.cvPublic:
					sw.Write("public");
					break;
				case CSharpVisibility.cvPrivate:
					sw.Write("private");
					break;
				case CSharpVisibility.cvProtected:
					sw.Write("protected");
					break;
				case CSharpVisibility.cvInternal:
					sw.Write("internal");
					break;
			}
		}
	}

	public class CNamespace {
		private CNamespace Parent { get; set; }
		public string Name;
		public CActualList<CNamespace> lChild = new CActualList<CNamespace>();
		public CNamespace(CNamespace parent, string name) {
			this.Parent = parent;
			if (this.Parent != null) {
				this.Parent.AddNamespace(this);
			}
			int PrimerPunto = name.IndexOf('.');
			if (PrimerPunto != -1) {
				string AntesPunto = name.Substring(0, PrimerPunto);
				string DespuesPunto = name.Substring(PrimerPunto + 1);
				this.Name = AntesPunto;
				new CNamespace(this, DespuesPunto);
			} else {
				this.Name = name;
			}
		}
		public CNamespace Actual {
			get {
				if (this.lChild.Count == 0) {
					return this;
				} else {
					return this.lChild.Actual.Actual;
				}
			}
		}
		public CNamespace AddNamespace(CNamespace Child) {
			return this.lChild.AddR(Child);
		}
		public string FullName {
			get {
				if (Parent == null) {
					return this.Name;
				} else {
					return this.Parent.FullName + "." + Name;
				}
			}
		}
	}

	public abstract class CSharpElement : ISourceGenerable {
		public abstract void Generate(CSourceWriter sw);
		public LUsing lUsing = new LUsing();
	}
	public class CSharpFile : CSourceFile, IStorableInDirectory {
		public CActualList<CSharpElement> lElement = new CActualList<CSharpElement>();
		private string _Name;
		public string Name {
			get {
				if (_Name.EndsWith(".cs")) {
					return _Name;
				}
				return _Name + ".cs";
			}
			set {
				this._Name = value;
			}
		}
		public string Namespace; //Es una aberración asimilar namespace con fichero pero simplifica mucho las cosas (Pareto: con 20% de esfuerzo 80% de resultados)

		public CSharpFile(string name, string namespaceName) {
			this.Name = name;
			this.Namespace = namespaceName;
		}
		public CSharpFile(string name, CUsing namespaceToUse) : this(name, namespaceToUse.Text) { }

		public CClass AddClass(string name, bool isStatic = false, bool isPartial = false) {
			return (CClass)this.lElement.AddR(new CClass(name, isStatic, isPartial));
		}

		public override void Generate(TextWriter lMensaje) {
			CSourceWriter sw = new CSourceWriter(this.ParentDirectory.FullName, this.Name);
			try {
				GenerateUsings(sw);
				GenerateNamespace(sw);
				foreach (CSharpElement item in this.lElement) {
					item.Generate(sw);
				}
				GenerateEndNamespace(sw);
			} finally {
				sw.Close();
				lMensaje.WriteLine("Generado: " + sw.FullFileName);
			}
		}

		private void GenerateNamespace(CSourceWriter sw) {
			sw.Write("namespace");
			sw.Space();
			sw.Write(this.Namespace);
			sw.OpenBracesLn();
			sw.AddTab();
		}
		private void GenerateEndNamespace(CSourceWriter sw) {
			sw.DelTab();
			sw.CloseBracesLn();
		}
		private void GenerateUsings(CSourceWriter sw) {
			bool escribioUsings = false;
			LUsing l = new LUsing();
			foreach (var item in this.lElement) {
				l.AddLUsing(item.lUsing);
			}
			foreach (CUsing u in l) {
				u.Generate(sw);
				escribioUsings = true;
			}
			if (escribioUsings) {
				sw.EndLine();
			}
		}
	}

	public class LUsing : List<CUsing> {
		public new void Add(CUsing u) {//Simplemente no lo añade si ya existe
			int i = 0;
			while (i < this.Count) {
				if (this[i].Text.Equals(u.Text)) {
					return;
				}
				i++;
			}
			base.Add(u);
		}
		public void AddLUsing(LUsing l) {
			foreach (CUsing item in l) {
				this.Add(item);
			}
		}
	}
	public class CUsing : ISourceGenerable {
		public string Text;
		public CUsing(string text) {
			this.Text = text;
		}
		public void Generate(CSourceWriter sw) {
			sw.WriteLn($"using {this.Text};");
		}
	}
	public static class CSharpStandarUsing {
		public static readonly CUsing System = new CUsing("System");
		public static readonly CUsing System_Data_SqlClient = new CUsing("System.Data.SqlClient");
		public static readonly CUsing System_Collections_Generic = new CUsing("System.Collections.Generic");
	}

	public class CClass : CSharpElement {
		public CSharpVisibility CSharpVisibility = CSharpVisibility.cvPublic;
		public string Name;
		public bool IsStatic;
		public bool IsPartial;
		public CActualList<CClassElement> lElement = new CActualList<CClassElement>();

		public string ParentClassName { get; set; }

		public CClass(string name, bool isStatic = false, bool isPartial = false) {
			this.Name = name;
			this.IsStatic = isStatic;
			this.IsPartial = isPartial;
		}

		public override void Generate(CSourceWriter sw) {
			CSharpProject.WriteVisibility(sw, this.CSharpVisibility);
			sw.Space();
			if (this.IsStatic) {
				sw.Write("static");
				sw.Space();
			}
			if (this.IsPartial) {
				sw.Write("partial");
				sw.Space();
			}
			sw.Write("class");
			sw.Space();
			sw.Write(this.Name);
			if (!string.IsNullOrWhiteSpace(this.ParentClassName)) {
				sw.Write(" : ");
				sw.Write(this.ParentClassName);
			}
			//*** Interfaces
			sw.OpenBracesLn();
			sw.AddTab();
			foreach (CClassElement item in this.lElement) {
				item.Generate(sw);
			}
			sw.DelTab();
			sw.CloseBracesLn();
		}

		public CField AddField(CField field) {
			return (CField)this.lElement.AddR(field);
		}

		public CMethod AddMethod(CMethod m) {
			return (CMethod)this.lElement.AddR(m);
		}

		public CConstructor AddConstructor(CConstructor cons) {
			return (CConstructor)this.lElement.AddR(cons);
		}

		public CProperty AddProperty(CProperty cProperty) {
			return (CProperty)this.lElement.AddR(cProperty);
		}
	}
	public abstract class CClassElement : ISourceGenerable {
		public abstract void Generate(CSourceWriter sw);
	}
	public class CProperty : CClassElement, ISourceGenerable {
		CSharpVisibility Visibility = CSharpVisibility.cvPublic;
		public string Name;
		public CType Type;
		bool HasGet = false;
		CSharpVisibility GetVisibility = CSharpVisibility.cvPublic;
		bool HasSet = false;
		CSharpVisibility SetVisibility = CSharpVisibility.cvProtected;
		public List<CSentence> lSentenceGet = new List<CSentence>();
		public List<CSentence> lSentenceSet = new List<CSentence>();

		public bool Override = false;

		public CProperty(string name, CSharpVisibility visibility, CType type, bool hasGet, CSharpVisibility getVisibility = CSharpVisibility.cvPublic, bool hasSet = false, CSharpVisibility setVisibility = CSharpVisibility.cvProtected) {
			this.Visibility = visibility;
			this.Name = name;
			this.Type = type;
			this.HasGet = hasGet;
			this.GetVisibility = getVisibility;
			this.HasSet = hasSet;
			this.SetVisibility = setVisibility;
		}
		public override void Generate(CSourceWriter sw) {
			CSharpProject.WriteVisibility(sw, this.Visibility);
			sw.Space();
			if (this.Override) {
				sw.Write("override");
				sw.Space();
			}
			this.Type.Generate(sw);
			sw.Space();
			sw.Write(this.Name);
			sw.OpenBracesLn();
			sw.AddTab();
			if (this.HasGet) {
				if (this.Visibility != this.GetVisibility) {
					CSharpProject.WriteVisibility(sw, this.GetVisibility);
					sw.Space();
				}
				sw.Write("get");
				if (this.lSentenceGet.Count > 0) {
					sw.OpenBracesLn();
					sw.AddTab();
					foreach(CSentence s in this.lSentenceGet) {
						s.Generate(sw);
					}
					sw.DelTab();
					sw.CloseBracesLn();
				} else {
					sw.SemicolonLn();
				}
			}
			if (this.HasSet) {
				if (this.Visibility != this.SetVisibility) {
					CSharpProject.WriteVisibility(sw, this.SetVisibility);
					sw.Space();
				}
				sw.Write("set");
				if (this.lSentenceSet.Count > 0) {
					sw.OpenBracesLn();
					sw.AddTab();
					foreach(CSentence s in this.lSentenceSet) {
						s.Generate(sw);
					}
					sw.DelTab();
					sw.CloseBracesLn();
				} else {
					sw.SemicolonLn();
				}
			}
			sw.DelTab();
			sw.CloseBracesLn();
		}
	}
	public class CField : CClassElement, ISourceGenerable {
		public CSharpVisibility CSharpVisibility = CSharpVisibility.cvPrivate;
		public bool IsStatic = false;
		public CType Type;
		public string Name;
		public string InitialValue;
		public CField(string name, CType type, CSharpVisibility visibility = CSharpVisibility.cvPublic, bool isStatic = false, string InitialValue = "") {
			this.Name = name;
			this.Type = type;
			this.CSharpVisibility = visibility;
			this.IsStatic = isStatic;
			this.InitialValue = InitialValue;
		}

		public override void Generate(CSourceWriter sw) {
			CSharpProject.WriteVisibility(sw, this.CSharpVisibility);
			sw.Space();
			if (this.IsStatic) {
				sw.Write("static");
				sw.Space();
			}
			this.Type.Generate(sw);
			sw.Space();
			sw.Write(this.Name);
			if (!string.IsNullOrWhiteSpace(this.InitialValue)) {
				sw.Write(" = ");
				sw.Write(this.InitialValue);
			}
			sw.SemicolonLn();
		}
	}
	public class CConstructor : CClassElement, ISourceGenerable {
		public CClass Class;
		public CSharpVisibility Visibility = CSharpVisibility.cvPublic;
		public List<CParam> lParam = new List<CParam>();
		public List<CSentence> lSentence = new List<CSentence>();
		//*** Esto te ha quedado raro. El constructor de CConstructor guarda una referencia a la clase pero el método (CMethod) no. Es incongruente.
		public CConstructor(CClass Class, CSharpVisibility visibility) {
			this.Class = Class;
			this.Visibility = visibility;
		}
		public void AddSentence(CSentence sentence) {
			this.lSentence.Add(sentence);
		}
		public override void Generate(CSourceWriter sw) {
			CSharpProject.WriteVisibility(sw, this.Visibility);
			sw.Space();
			if (this.Class == null) {
				throw new NullReferenceException("El constructor ha de tener una clase.");
			}
			sw.Write(this.Class.Name);
			sw.Write("(");
			bool Primero = true;
			foreach (CParam p in this.lParam) {
				sw.WriteComma(ref Primero);
				p.Generate(sw);
			}
			sw.Write(")");
			sw.OpenBracesLn();
			sw.AddTab();
			foreach (CSentence sen in lSentence) {
				sen.Generate(sw);
			}
			sw.DelTab();
			sw.CloseBracesLn();
		}

		public void AddParam(string Name, CType type) {
			this.lParam.Add(new CParam(Name, type));
		}
	}
	public class CMethod : CClassElement, ISourceGenerable {
		public CSharpVisibility Visibility = CSharpVisibility.cvPublic;
		public bool IsStatic = false;
		public bool IsOverride= false;
		public CType Type;
		public string Name;
		public List<CParam> lParam = new List<CParam>();
		public List<CSentence> lSentence = new List<CSentence>();

		public CMethod(string name, CType type, CSharpVisibility visibility, bool isStatic = false,bool isOverride = false) {
			this.Name = name;
			this.Type = type;
			this.Visibility = visibility;
			this.IsStatic = isStatic;
			this.IsOverride = isOverride;
		}
		public void AddSentence(CSentence sentence) {
			this.lSentence.Add(sentence);
		}

		public override void Generate(CSourceWriter sw) {
			CSharpProject.WriteVisibility(sw, this.Visibility);
			sw.Space();
			if (this.IsStatic) {
				sw.Write("static");
				sw.Space();
			}
			if (this.IsOverride) {
				sw.Write("override");
				sw.Space();
			}
			this.Type.Generate(sw);
			sw.Space();
			sw.Write(this.Name);
			sw.Write("(");
			bool Primero = true;
			foreach (CParam p in this.lParam) {
				sw.WriteComma(ref Primero);
				p.Generate(sw);
			}
			sw.Write(")");
			sw.WriteLn("{");
			sw.AddTab();
			foreach (CSentence sen in lSentence) {
				sen.Generate(sw);
			}
			sw.DelTab();
			sw.WriteLn("}");
		}

		public void AddParam(string Name, CType type) {
			this.lParam.Add(new CParam(Name, type));
		}
	}

	public abstract class CSentence : ISourceGenerable {
		public abstract void Generate(CSourceWriter sw);
	}
	public class CTextualSentence : CSentence {
		string Text;
		public override void Generate(CSourceWriter sw) {
			sw.WriteLn(this.Text);
		}
		public CTextualSentence(string text) {
			this.Text = text;
		}
	}

	public class CParam : ISourceGenerable {
		private string Name;
		private CType Type;

		public CParam(string name, CType type) {
			this.Name = name;
			this.Type = type;
		}

		public void Generate(CSourceWriter sw) {
			Type.Generate(sw);
			sw.Space();
			sw.Write(this.Name);
		}
	}
	public abstract class CType : ISourceGenerable {
		public abstract void Generate(CSourceWriter sw);
	}
	public class TextualType : CType {
		string Text { get; set; }
		public override void Generate(CSourceWriter sw) {
			sw.Write(this.Text);
		}
		public TextualType(string text) {
			this.Text = text;
		}
	}
	public abstract class CSharpBasePrimitiveType : CType {

		public class CVoid : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("void");
			}
		}
		public class CByte : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("byte");
			}
		}
		public class CByteNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("byte?");
			}
		}
		public class CShort : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("short");
			}
		}
		public class CShortNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("short?");
			}
		}
		public class CInt : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("int");
			}
		}
		public class CIntNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("int?");
			}
		}
		public class CBool : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("bool");
			}
		}
		public class CBoolNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("bool?");
			}
		}
		public class CFloat : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("float");
			}
		}
		public class CFloatNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("float?");
			}
		}
		public class CDouble : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("double");
			}
		}
		public class CDoubleNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("double?");
			}
		}
		public class CDate : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("DateTime");
			}
		}
		public class CDateNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("DateTime?");
			}
		}
		public class CDateTime : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("DateTime");
			}
		}
		public class CDateTimeNullable : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("DateTime ?");
			}
		}
		public class CString : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("string");
			}
		}
		public class CArrayBytes : CSharpBasePrimitiveType {
			public override void Generate(CSourceWriter sw) {
				sw.Write("byte[]");
			}
		}

	}
	public static class CSharpPrimitiveType {
		public static readonly CSharpBasePrimitiveType.CVoid cVoid = new CSharpBasePrimitiveType.CVoid();
		public static readonly CSharpBasePrimitiveType.CByte cByte = new CSharpBasePrimitiveType.CByte();
		public static readonly CSharpBasePrimitiveType.CByteNullable cByteNullable = new CSharpBasePrimitiveType.CByteNullable();
		public static readonly CSharpBasePrimitiveType.CDateTime cDateTime = new CSharpBasePrimitiveType.CDateTime();
		public static readonly CSharpBasePrimitiveType.CDateTimeNullable cDateTimeNullable = new CSharpBasePrimitiveType.CDateTimeNullable();
		public static readonly CSharpBasePrimitiveType.CInt cInt = new CSharpBasePrimitiveType.CInt();
		public static readonly CSharpBasePrimitiveType.CIntNullable cIntNullable = new CSharpBasePrimitiveType.CIntNullable();
		public static readonly CSharpBasePrimitiveType.CShort cShort = new CSharpBasePrimitiveType.CShort();
		public static readonly CSharpBasePrimitiveType.CShortNullable cShortNullable = new CSharpBasePrimitiveType.CShortNullable();
		public static readonly CSharpBasePrimitiveType.CString cString = new CSharpBasePrimitiveType.CString();
		public static readonly CSharpBasePrimitiveType.CArrayBytes cArrayBytes = new CSharpBasePrimitiveType.CArrayBytes();
		public static readonly CSharpBasePrimitiveType.CBool cBool = new CSharpBasePrimitiveType.CBool();
		public static readonly CSharpBasePrimitiveType.CBoolNullable cBoolNullable = new CSharpBasePrimitiveType.CBoolNullable();
		public static readonly CSharpBasePrimitiveType.CFloat cFloat = new CSharpBasePrimitiveType.CFloat();
		public static readonly CSharpBasePrimitiveType.CFloatNullable cFloatNullable = new CSharpBasePrimitiveType.CFloatNullable();
		public static readonly CSharpBasePrimitiveType.CDouble cDouble = new CSharpBasePrimitiveType.CDouble();
		public static readonly CSharpBasePrimitiveType.CDoubleNullable cDoubleNullable = new CSharpBasePrimitiveType.CDoubleNullable();
		public static readonly CSharpBasePrimitiveType.CDate cDate = new CSharpBasePrimitiveType.CDate();
		public static readonly CSharpBasePrimitiveType.CDateNullable cDateNullable = new CSharpBasePrimitiveType.CDateNullable();
	}

	public enum CSharpVisibility { cvPublic, cvPrivate, cvProtected, cvInternal }
}
