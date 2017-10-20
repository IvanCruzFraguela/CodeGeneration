using System;
using System.Collections.Generic;
using System.IO;

namespace IvanCruz.CodeGeneration.SourceGeneration {
	public class CSourceWriter : IDisposable {
		protected byte _tabs = 0;
		protected bool isNewLine = true;
		protected string _FullFileName;
		public string FullFileName {
			get {
				return _FullFileName;
			}
			protected set {
				_FullFileName = value;
			}
		}
		private StreamWriter sw;

		public CSourceWriter(string BaseDir, string FileName) {
			this.FullFileName = Path.Combine(BaseDir, FileName);
		}
		public CSourceWriter(Stream s) {
			sw = new StreamWriter(s);
		}

		public CSourceWriter(string FileName) {
			this.FullFileName = FileName;
		}

		public void AddTab() {
			_tabs++;
		}
		public void DelTab() {
			if (_tabs == 0) {
				throw new Exception("Se han bajado los tabs cuando ya estaban a 0");
			}
			_tabs--;
		}
		public void Write(string cad) {
			if (sw == null) {
				this.GenerateFile();
			}
			if (isNewLine) {
				for (byte i = 0; i < _tabs; i++) {
					this.sw.Write("\t");
				}
				isNewLine = false;
			}
			this.sw.Write(cad);
		}

		public void Semicolon() {
			this.sw.Write(";");
		}

		public void Space() {
			this.sw.Write(" ");
		}

		private void GenerateFile() {
			if (FullFileName == null) {
				throw new ArgumentNullException("No se ha proporcionado nombre de fichero");
			}
			if (!Directory.Exists(Path.GetDirectoryName(FullFileName))) {
				Directory.CreateDirectory(Path.GetDirectoryName(FullFileName));
			}
			//Perdemos la referencia al FileStream. A ver si no da problemas.
			FileStream fs = new FileStream(this.FullFileName, FileMode.Create);
			this.sw = new StreamWriter(fs);
		}

		public void OpenBraces() {
			this.Write("{");
		}
		public void CloseBraces() {
			this.Write("}");
		}

		public void SemicolonLn() {
			this.Semicolon();
			this.EndLine();
		}

		public void OpenParenthesis() {
			this.Write("(");
		}

		public void WriteQuoted(string value) {
			this.Write("\"" + value + "\"");
		}

		public void CloseParenthesis() {
			this.Write(")");
		}
		public void WriteLn(string cad) {
			this.Write(cad);
			this.sw.WriteLine();
			isNewLine = true;
		}

		public void EndLine() {
			this.WriteLn("");
		}
		public void WriteCommaStringList(List<string> lString) {
			bool primero = true;
			foreach (string c in lString) {
				this.WriteComma(ref primero);
				this.WriteLn(c);
			}
		}
		public void WriteComma(ref bool primero) {
			if (primero) {
				primero = false;
			} else {
				this.Write(",");
			}
		}
		public void WriteAnd(ref bool primero) {
			if (primero) {
				primero = false;
			} else {
				this.Write(" and ");
			}
		}
		public void Close() {
			this.sw.Close();
		}

		public void OpenBracesLn() {
			this.OpenBraces();
			this.EndLine();
		}
		public void CloseBracesLn() {
			this.CloseBraces();
			this.EndLine();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					if (this.sw != null) {
						this.sw.Dispose();
					}
				}
				disposedValue = true;
			}
		}
		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
