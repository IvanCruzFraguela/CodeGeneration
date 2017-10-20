using IvanCruz.CodeGeneration.SMD;
using IvanCruz.CodeGeneration.SourceGeneration;
using IvanCruz.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace IvanCruz.CodeGeneration.LibDB1 {
	public class CDBM {
		private string _name;
		private List<CTableM> _lTable;
		public List<CTableM> lTable {
			get {
				if (_lTable == null) {
					_lTable = new List<CTableM>();
				}
				return _lTable;
			}
		}
		public CTableM ActualTable { get; protected set; }
		public CFieldM ActualField { get; protected set; }
		public CRelationM ActualRelation { get; protected set; }
		public string Name { get { return _name; } set { _name = value; } }

		public string Description { get; set; }

		public CTableM AddTable(string Name, string singularTitle = null, string pluralTitle = null, EGender gender = EGender.male) {
			CTableM result = new CTableM(Name, singularTitle, pluralTitle, gender);
			this.ActualTable = result;
			this.lTable.Add(result);
			return result;
		}
		public CFieldM AddField(string name, string title = "", SqlDbType td = SqlDbType.Int, int lenght = 0, bool isPrimaryKey = false, bool IsNullable = true, bool IsIdentity = false, int defaultWidth = 100, bool ShowInGrid = true) {
			return this.ActualField = this.ActualTable.AddField(name, title, td, lenght, isPrimaryKey, IsNullable, IsIdentity, defaultWidth, ShowInGrid);
		}

		public void AddTableDesc(string desc) {
			if (this.ActualTable == null) {
				throw new Exception("No hay tabla actual definida. No se puede Describir la tabla actual.");
			}
			this.AddTableDesc(this.ActualTable, desc);
		}

		public void AddFieldDesc(string desc) {
			if (this.ActualField == null) {
				throw new Exception("No hay field actuale definido. No se puede describir el field actual.");
			}
			this.AddFieldDesc(this.ActualField, desc);
		}

		private void AddFieldDesc(CFieldM field, string desc) {
			if (field == null) {
				throw new ArgumentNullException("El fiel a describir no puede ser nulo");
			}
			field.Desc = desc;
		}

		private void AddTableDesc(CTableM table, string desc) {
			if (table == null) {
				throw new ArgumentNullException("La tabla a describir no puede ser nula");
			}
			table.Desc = desc;
		}

		public CFieldM AddFieldId(SqlDbType TD, bool Autonumeric) {
			CFieldM result = this.AddField(SSN.Id, "", TD, 0, true, true, Autonumeric);
			result.ShowInGrid = false;
			return result;
		}

		public CFieldM AddFieldId(SqlDbType TD) {
			return this.AddFieldId(TD, true);
		}
		public CFieldM AddFieldId() {
			return AddFieldId(SqlDbType.Int, true);
		}
		public CFieldM AddFieldNomb(int lenght) {
			int defaultWidth = lenght * 10;
			defaultWidth = Math.Min(defaultWidth, 500);
			return this.AddField(SSN.Nomb, "Nombre", SqlDbType.VarChar, lenght, isPrimaryKey: false, IsNullable: true, IsIdentity: false, defaultWidth: defaultWidth, ShowInGrid: true);
		}

		public void AddForeignKey(string tableDestination) {
			CRelationM r = new CRelationM();
			r.tableDestination = tableDestination;
			this.ActualRelation = r;
			r.tableSource = this.ActualTable;
			this.ActualTable.AddRelation(r);
		}
		public void AddForeignKeyField(string fieldName) {
			if (this.ActualRelation == null) {
				throw new Exception("Es necesario crear antes una ForeignKey");
			}
			if (this.ActualRelation.lFieldSource == null) {
				this.ActualRelation.lFieldSource = new List<string>();
			}
			this.ActualRelation.lFieldSource.Add(fieldName);
		}
		public void AddForeignKeyReferenced(string fieldName) {
			if (this.ActualRelation == null) {
				throw new Exception("Es necesario crear antes una ForeignKey");
			}
			if (this.ActualRelation.lFieldDestination == null) {
				this.ActualRelation.lFieldDestination = new List<string>();
			}
			this.ActualRelation.lFieldDestination.Add(fieldName);
		}

		public void AddForeignKey(string[] AFields, string tableDestination, string[] AReferenced) {
			if (AFields.Length != AReferenced.Length) {
				throw new Exception("Error: cantidad de campos distinto entre los que referencian y los referenciados");
			}
			this.AddForeignKey(tableDestination);
			foreach (string s in AFields) {
				this.AddForeignKeyField(s);
			}
			foreach (string s in AReferenced) {
				this.AddForeignKeyReferenced(s);
			}
		}

		public string GenerateSql(bool withDrops) {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			if (withDrops) {
				//Primero se borran todas las relaciones para evitar errores de eliminación de tablas con otras relacionadas.(Si existen)
				foreach (CTableM t in this.lTable) {
					foreach (CRelationM r in t.lRelation) {
						sw.WriteLn(string.Format("IF OBJECT_ID('dbo.{0}', 'F') IS NOT NULL ALTER TABLE dbo.{1} DROP CONSTRAINT {0};", r.Name, r.tableSource.Name));
						sw.WriteLn("GO");
					}
				}
				//Se elinan las tablas si existen
				foreach (CTableM t in this.lTable) {
					sw.WriteLn(string.Format("IF OBJECT_ID('dbo.{0}', 'U') IS NOT NULL DROP TABLE dbo.{0};", t.Name));
					sw.WriteLn("GO");
				}
			}
			foreach (CTableM t in this.lTable) {
				t.GenerateSql(sw);
				sw.EndLine();
			}

			foreach (CTableM t in this.lTable) {
				foreach (CRelationM r in t.lRelation) {
					r.GeneraSql(sw);
				}
			}

			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}

		public void AddUniqueConstraint() {
			this.AddUniqueConstraint(this.ActualTable, new List<CFieldM> { this.ActualField });
		}

		private void AddUniqueConstraint(CTableM table, string[] fieldNames) {
			List<CFieldM> lFields = new List<CFieldM>();
			foreach (string name in fieldNames) {
				lFields.Add(table.FindField(name));
			}
			AddUniqueConstraint(table, lFields);
		}

		private void AddUniqueConstraint(CTableM table, List<CFieldM> lFields) {
			CUniqueConstraintM u = new CUniqueConstraintM(table, lFields);
		}

		public static string PrimeraMayus(string cad) {
			if (string.IsNullOrEmpty(cad)) {
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(cad.Substring(0, 1).ToUpper());
			sb.Append(cad.Substring(1));
			return sb.ToString();
		}
		public void AddForeignKeyForLastField(string TableReferencedName) {
			this.AddForeignKey(this.ActualField.Name, TableReferencedName, SSN.Id);
		}
		private void AddForeignKey(string FieldName, string tableReferencedName, string ReferencedFieldName) {
			this.AddForeignKey(tableReferencedName);
			this.AddForeignKeyField(FieldName);
			this.AddForeignKeyReferenced(ReferencedFieldName);
		}
		public void GenerateDescription(StreamWriter sw) {
			sw.WriteLine("<HTML>");
			sw.WriteLine("<BODY>");
			sw.WriteLine("<H1>");
			sw.WriteLine("Base de datos: " + this.Name);
			sw.WriteLine("</H1>");
			sw.WriteLine("<p>");
			sw.WriteLine(this.Description);
			sw.WriteLine("</p>");
			foreach (CTableM tabla in this.lTable) {
				tabla.GenerateDescription(sw);
			}
			sw.WriteLine("</BODY>");
			sw.WriteLine("</HTML>");
		}
		/// <summary>
		/// Ordena las tablas de modo que las tablas referenciadas por claves foraneas siempre estén antes de las que referencian
		/// </summary>
		public void OrdenaTablas() {
			List<CTableM> listaOrdenada = new List<CTableM>();
			while (this.lTable.Count > 0) {//mientras haya elementos
				ColocaTablaEnOrden(this.lTable, this.lTable[0], listaOrdenada);
			}
			foreach (var item in listaOrdenada) {
				lTable.Add(item);
			}
		}
		private void ColocaTablaEnOrden(List<CTableM> listaOriginal, CTableM cTableM, List<CTableM> listaOrdenada) {
			listaOriginal.Remove(cTableM);
			foreach (CRelationM relacion in cTableM.lRelation) {
				string NombreTablaConLaQueSeRelaciona = relacion.tableDestination;
				CTableM tablaRelacionada = listaOriginal.Find(x => x.Name.Equals(NombreTablaConLaQueSeRelaciona));
				if (tablaRelacionada != null) {//Si la tabla relacionada aún está en la lista de tablas a ordenar
											   //Meter antes en la lista de ordenadas
					ColocaTablaEnOrden(listaOriginal, tablaRelacionada, listaOrdenada);
				}
			}
			listaOrdenada.Add(cTableM);
		}
		public CTableM GetTable(string tableName) {
			foreach (var t in this.lTable) {
				if (t.Name.Equals(tableName)) {
					return t;
				}
			}
			return null;
		}
	}
	public class CUniqueConstraintM {
		protected CTableM _table;
		private string _nombre;
		private List<CFieldM> _lFields;
		protected List<CFieldM> LFields { get { return _lFields; } set { _lFields = value; } }

		public CUniqueConstraintM(CTableM table, List<CFieldM> lFields) {
			this.Table = table;
			this.LFields = lFields;
		}

		public string Nombre {
			get {
				if (string.IsNullOrWhiteSpace(_nombre)) {
					StringBuilder sb = new StringBuilder();
					sb.Append("Un_");
					sb.Append(CDBM.PrimeraMayus(this.Table.Name));
					foreach (CFieldM f in this.LFields) {
						sb.Append(CDBM.PrimeraMayus(f.Name));
					}
					return sb.ToString();
				}
				return _nombre;
			}

			set {
				this._nombre = value;
			}
		}

		public CTableM Table {
			get {
				return _table;
			}

			set {
				if (_table != value) {
					if (_table != null) {
						_table.RemoveUniqueConstraint(this);
					}
					if (value != null) {
						value.AddUniqueConstraint(this);
					}
					_table = value;
				}
			}
		}

		internal void GeneraSql(CSourceWriter sw) {
			bool primero = true;
			//CONSTRAINT AK_TransactionID UNIQUE(TransactionID)
			sw.WriteLn(",constraint " + this.Nombre + " unique (");
			sw.AddTab();
			foreach (CFieldM f in this.LFields) {
				sw.WriteComma(ref primero);
				sw.WriteLn("" + f.Name);
			}
			sw.DelTab();
			sw.WriteLn(")");
		}
	}

	public enum EGender { male, female }
	public class CTableM {
		public string Name;
		protected string _SingularTitle;
		public string SingularTitle {
			get {
				if (String.IsNullOrWhiteSpace(this._SingularTitle)) {
					return Name;
				} else {
					return _SingularTitle;
				}
			}
			set {
				this._SingularTitle = value;
			}
		}
		protected string _PluralTitle;
		public string PluralTitle {
			get {
				if (String.IsNullOrWhiteSpace(this._PluralTitle)) {
					return this.SingularTitle;
				} else {
					return this._PluralTitle;
				}
			}
			set {
				this._PluralTitle = value;
			}
		}
		protected EGender _Gender = EGender.male;
		public EGender Gender {
			get {
				return _Gender;
			}
			set {
				_Gender = value;
			}
		}

		public string Desc { get; set; }
		//Hay que tener una lista de campos con la clave primaria porque en Informix me he encontrado con claves primarias con orden distinto al de los campos y que no son los primeros de la tabla
		public readonly List<CFieldM> lFieldPk = new List<CFieldM>();
		public readonly List<CFieldM> lFieldData = new List<CFieldM>();
		public readonly List<CFieldM> lFieldAll = new List<CFieldM>();
		public readonly List<CRelationM> lRelation = new List<CRelationM>();
		private List<CUniqueConstraintM> _lUniqueConstraint;
		#region Esto quiero que esté en otra clase
		public bool GeneraVerTodos { get; set; }
		public bool GeneraBuscar { get; set; }
		public bool GeneraCrear { get; set; }
		#endregion
		public CTableM(string Name, string singularTitle = null, string pluralTitle = null, EGender gender = EGender.male) {
			this.Name = Name;
			this.SingularTitle = singularTitle;
			this.PluralTitle = pluralTitle;
			this.Gender = Gender;
		}
		public string GeneraDelete() {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			this.GeneraDelete(sw);

			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}
		private void GeneraDelete(CSourceWriter sw) {
			sw.Write("delete from ");
			sw.Write(this.Name);
			sw.Write(" where ");
			bool Primero = true;
			foreach (CFieldM f in this.lFieldPk) {
				sw.WriteAnd(ref Primero);
				sw.Write(f.Name);
				sw.Write(" = ");
				sw.Write(SUtilBD.Param(f.Name));
			}
		}
		public string GeneraSelectByPk() {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			this.GeneraSelectByPk(sw);
			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}
		private void GeneraSelectByPk(CSourceWriter sw) {
			sw.Write("select");
			sw.Space();
			this.CamposPorComas(sw);
			sw.Space();
			sw.Write("from");
			sw.Space();
			sw.Write(this.Name);
			bool Primero = true;
			var sb = new StringBuilder();
			foreach (CFieldM f in this.lFieldPk) {
				SUtilBD.AddCondicion(ref Primero, sb, f.Name + " = @" + SUtilBD.ParamName(f.Name));
			}
			sw.Space();
			sw.Write(sb.ToString());
		}
		public List<CUniqueConstraintM> LUniqueConstraint {
			get {
				if (_lUniqueConstraint == null) {
					_lUniqueConstraint = new List<CUniqueConstraintM>();
				}
				return _lUniqueConstraint;
			}
			set {
				this._lUniqueConstraint = value;
			}
		}

		public bool TienePropiedadIdentidad {
			get {
				foreach (CFieldM item in this.lFieldAll) {
					if (item.IsIdentity) {
						return true;
					}
				}
				return false;
			}
		}

		public CFieldM AddField(string name, string titulo, SqlDbType td, int largo, bool isPrimaryKey, bool IsNullable, bool IsIdentity, int defaultWidth = 100, bool ShowInGrid = true) {
			CFieldM result = new CFieldM(this, name, titulo, td, largo, isPrimaryKey, IsNullable, IsIdentity, defaultWidth, ShowInGrid);
			this.lFieldAll.Add(result);
			if (isPrimaryKey) {
				this.lFieldPk.Add(result);
			} else {
				this.lFieldData.Add(result);
			}
			return result;
		}
		internal void AddRelation(CRelationM r) {
			lRelation.Add(r);
		}
		internal void AddUniqueConstraint(CUniqueConstraintM uniqueConstraint) {
			this.LUniqueConstraint.Add(uniqueConstraint);
		}
		internal CFieldM FindField(string name) {
			int i = lFieldAll.Count - 1;
			while (i >= 0) {
				if (this.lFieldAll[i].Name.Equals(name)) {
					return this.lFieldAll[i];
				}
				i--;
			}
			throw new Exception("Campo " + name + " no encontrado");
		}
		internal void GenerateSql(CSourceWriter sw) {
			sw.WriteLn("create table dbo." + this.Name + "(");
			bool primero = true;
			sw.AddTab();
			foreach (CFieldM f in this.lFieldAll) {
				f.GeneraSql(sw, ref primero);
			}
			if (this.lFieldPk.Count > 0) {
				sw.WriteLn(",CONSTRAINT PK_" + this.Name + " PRIMARY KEY (");
				primero = true;
				sw.AddTab();
				foreach (CFieldM f in this.lFieldPk) {
					sw.Write("");
					sw.WriteComma(ref primero);
					sw.WriteLn(f.Name);
				}
				sw.DelTab();
				sw.WriteLn(")");
			}
			foreach (CUniqueConstraintM u in this.LUniqueConstraint) {
				u.GeneraSql(sw);
			}
			sw.DelTab();
			sw.WriteLn(")");

			sw.EndLine();
			sw.WriteLn("GO");
			//Genera las unique constraints
		}
		internal void RemoveUniqueConstraint(CUniqueConstraintM uniqueConstraint) {
			this.LUniqueConstraint.Remove(uniqueConstraint);
		}
		public string GeneraSelectAll() {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			this.GeneraSelectAll(sw);

			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}
		private void GeneraSelectAll(CSourceWriter sw) {
			sw.Write("select");
			sw.Space();
			this.CamposPorComas(sw);
			sw.Space();
			sw.Write("from");
			sw.Space();
			sw.Write(this.Name);
		}
		private void CamposPorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				sw.WriteComma(ref Primero);
				sw.Write(f.Name);
			}
		}
		public string GeneraInsert() {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			this.GeneraInsert(sw);

			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}
		private void ParametrosCamposNoIdentityPorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				if (!f.IsIdentity) {
					sw.WriteComma(ref Primero);
					sw.Write(SUtilBD.Param(f.Name));
				}
			}
		}
		private void CamposNoIdentityPorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				if (!f.IsIdentity) {
					sw.WriteComma(ref Primero);
					sw.Write(f.Name);
				}
			}
		}
		public void GeneraInsert(CSourceWriter sw) {
			sw.Write("insert into ");
			sw.Write(this.Name);
			sw.Write("(");
			this.CamposNoIdPorComas(sw);
			sw.Write(")");
			sw.Write("values");
			sw.Write("(");
			this.ParametrosCamposNoIdPorComas(sw);
			sw.Write(")");
		}
		public string GeneraUpdate() {
			MemoryStream ms = new MemoryStream();
			CSourceWriter sw = new CSourceWriter(ms);
			this.GeneraUpdate(sw);

			sw.Close();
			return Encoding.UTF8.GetString(ms.ToArray());
		}
		private void GeneraUpdate(CSourceWriter sw) {
			sw.Write("update ");
			sw.Write(this.Name);
			sw.Write(" set ");
			bool Primero = true;
			foreach (CFieldM f in this.lFieldData) {
				sw.WriteComma(ref Primero);
				sw.Write(f.Name);
				sw.Write(" = ");
				sw.Write(SUtilBD.Param(f.Name));
			}
			sw.Write(" where ");
			Primero = true;
			foreach (CFieldM f in this.lFieldPk) {
				sw.WriteComma(ref Primero);
				sw.Write(f.Name);
				sw.Write(" = ");
				sw.Write(SUtilBD.Param(f.Name));
			}
		}
		private void CamposNoClavePorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldData) {
				sw.WriteComma(ref Primero);
				sw.Write(f.Name);
			}
		}
		private void CamposNoIdPorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				if (!f.IsIdentity) {
					sw.WriteComma(ref Primero);
					sw.Write(f.Name);
				}
			}
		}
		private void ParametrosCamposNoClavePorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldData) {
				sw.WriteComma(ref Primero);
				sw.Write(SUtilBD.Param(f.Name));
			}
		}
		private void ParametrosCamposNoIdPorComas(CSourceWriter sw) {
			bool Primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				if (!f.IsIdentity) {
					sw.WriteComma(ref Primero);
					sw.Write(SUtilBD.Param(f.Name));
				}
			}
		}
		public void GeneraParametersCSharp(CSourceWriter sw) {
			foreach (var f in this.lFieldData) {
				if (f.IsNullable) {
					sw.Write("com.Parameters.AddWithValue(UtilBD.Param(\"");
					sw.Write(f.Name);
					sw.Write("\"), ");
					sw.Write(f.Name);
					sw.WriteLn(");");
				} else {
					sw.Write("if (");
					sw.Write(f.Name);
					sw.Write(" == null){");
					sw.Write("com.Parameters.AddWithValue(UtilBD.Param(\"");
					sw.Write(f.Name);
					sw.Write("\"), ");
					sw.Write("DBNull.Value");
					sw.WriteLn(");");
					sw.WriteLn("} else {");
					sw.Write("com.Parameters.AddWithValue(UtilBD.Param(\"");
					sw.Write(f.Name);
					sw.Write("\"), ");
					sw.Write(f.Name);
					sw.WriteLn(");");
					sw.WriteLn("}");
				}
			}
		}
		public void GeneraInsertMethod(CSourceWriter sw) {
			sw.Write("public static int Insert");
			sw.Write(this.Name);
			sw.Write("(");
			bool primero = true;
			foreach (CFieldM f in this.lFieldAll) {
				if (!f.IsIdentity) {
					sw.WriteComma(ref primero);
					sw.Write(f.csDataTypeName());
					sw.Write(" ");
					sw.Write(f.Name);
				}
			}
			sw.WriteComma(ref primero);
			sw.Write("SqlConnection con");
			sw.Write(")");
			sw.WriteLn("{");
		}
		internal void GenerateDescription(StreamWriter sw) {
			sw.WriteLine("<H2>");
			sw.WriteLine("Tabla: " + this.Name);
			sw.WriteLine("</H2>");
			sw.WriteLine("<p>");
			sw.WriteLine(this.Desc);
			sw.WriteLine("</p>");
			sw.WriteLine("<ul>");
			sw.WriteLine("<H3>");
			sw.WriteLine("Campos");
			sw.WriteLine("</H3>");
			foreach (CFieldM field in this.lFieldAll) {
				field.GenerateDescription(sw);
			}
			sw.WriteLine("</ul>");
		}
	}
	public class CFieldM {
		public string Name;
		public string Title;
		public int DefaultWidth = 100;
		private bool _ShowInGrid = true;
		public bool ShowInGrid {
			get {
				return _ShowInGrid;
			}
			set {
				_ShowInGrid = value;
			}
		}
		public string Desc { get; set; }
		private SqlDbType _td;
		public int Lenght;
		public bool IsPrimaryKey {
			get {
				if (Table == null) {
					return false;
				}
				return Table.lFieldPk.Contains(this);
			}
		}
		private bool _isNullable;
		public bool IsNullable {
			get {
				if (IsPrimaryKey) {
					return false;
				}
				return _isNullable;
			}
			set {
				_isNullable = value;
			}
		}
		public bool IsIdentity;
		protected CTableM _table;
		internal CTableM Table {
			get {
				return _table;
			}
			set {
				_table = value;
			}
		}
		public CFieldM(CTableM table, string name, string title, SqlDbType td, int lenght, bool isPrimaryKey, bool isNullable, bool isIdentity, int defaultWidth = 100, bool showInGrid = true) {
			if ((td == SqlDbType.VarChar) && (lenght == 0)) {
				throw new Exception($"Error: No se puede tener un campo de tipo VarChar con longitud 0. Tabla {table.Name} campo {name}");
			}
			this.Table = table;
			this.Name = name;
			this.Title = title;
			this._td = td;
			this.Lenght = lenght;
			this.IsNullable = isNullable;
			this.IsIdentity = isIdentity;
			this.DefaultWidth = defaultWidth;
			this.ShowInGrid = showInGrid;
		}
		public string CadenaTDSqlServer {
			get {
				return this.Td.ToString();
			}
		}
		public SqlDbType Td {
			get {
				return _td;
			}
		}
		public string ParameterName {
			get {
				return SUtilBD.ParamName(this.Name);
			}
		}

		public string CadenaTDConvert {
			get {
				switch (this.Td) {
					case SqlDbType.Bit:
						return "ToBoolean";
					case SqlDbType.Date:
						return "ToDateTime";
					case SqlDbType.Float:
						return "ToDouble";//El float sql es un Double C#
					case SqlDbType.Int:
						return "ToInt32";
					case SqlDbType.SmallInt:
						return "ToInt16";
					case SqlDbType.TinyInt:
						return "ToByte";
					case SqlDbType.Text:
						return "ToString";
					case SqlDbType.VarChar:
						return "ToString";
					default:
						throw new Exception("Dato " + this.Td.GetType().FullName + " no considerado");
				}
			}
		}

		public void GeneraSql(CSourceWriter sw, ref bool primero) {
			sw.WriteComma(ref primero);
			sw.Write(this.Name);
			sw.Write(" ");
			sw.Write(this.CadenaTDSqlServer);
			if ((this.Td == SqlDbType.VarChar) || (this.Td == SqlDbType.VarBinary)) {
				sw.Write("(");
				sw.Write(this.Lenght.ToString());
				sw.Write(")");
			}
			if (this.IsIdentity) {
				sw.Write(" identity ");
			}
			if (!this.IsNullable) {
				sw.Write(" not null ");
			}
			sw.EndLine();
		}
		internal string csDataTypeName() {
			StringBuilder sb = new StringBuilder();
			switch (this.Td) {
				case SqlDbType.Bit:
					sb.Append("bool");
					if (this.IsNullable) {//Si es anulable y el tipo de dato un por valor (no es un puntero) añadir "?"
						sb.Append("?");
					}
					break;
				case SqlDbType.Date:
					sb.Append("DateTime");
					if (this.IsNullable) {//Si es anulable y el tipo de dato un por valor (no es un puntero) añadir "?"
						sb.Append("?");
					}
					break;
				case SqlDbType.Float:
					sb.Append("float");
					if (this.IsNullable) {//Si es anulable y el tipo de dato un por valor (no es un puntero) añadir "?"
						sb.Append("?");
					}
					break;
				case SqlDbType.Int:
					sb.Append("int");
					if (this.IsNullable) {//Si es anulable y el tipo de dato un por valor (no es un puntero) añadir "?"
						sb.Append("?");
					}
					break;
				case SqlDbType.SmallInt:
					sb.Append("Int16");
					if (this.IsNullable) {//Si es anulable y el tipo de dato un por valor (no es un puntero) añadir "?"
						sb.Append("?");
					}
					break;
				case SqlDbType.Text:
					sb.Append("string");
					break;
				case SqlDbType.VarChar:
					sb.Append("string");
					break;
				default: throw new Exception("Tipo de dato " + this.CadenaTDSqlServer + " no soportado para pasar a C#");
			}
			return sb.ToString();
		}
		internal void GenerateDescription(StreamWriter sw) {
			sw.WriteLine("<li>");
			sw.Write(this.Name);
			sw.Write(" (");
			sw.Write(this.CadenaTDSqlServer);
			if (this.Lenght > 0) {
				sw.Write(" - " + this.Lenght.ToString());
			}
			sw.Write(")");
			if (this.IsPrimaryKey) {
				sw.Write(" Primary Key");
			}
			if (this.IsNullable) {
				sw.Write(" Not Null");
			}
			if (this.IsIdentity) {
				sw.Write(" Identity");
			}
			sw.WriteLine("</li>");
			if (!String.IsNullOrWhiteSpace(this.Title) || !String.IsNullOrWhiteSpace(this.Desc)) {
				bool Escrito = false;
				if (!String.IsNullOrWhiteSpace(this.Title)) {
					sw.Write(this.Title);
					Escrito = true;
				}
				if (!String.IsNullOrWhiteSpace(this.Desc)) {
					if (Escrito) {
						sw.Write(" : ");
					}
					sw.Write(this.Desc);
				}
			}
		}
	}
	public class CRelationM {
		private string _name;
		public CTableM tableSource;
		public string tableDestination;
		public List<string> lFieldSource = new List<string>();
		internal protected List<string> lFieldDestination;

		public string Name {
			get {
				if (string.IsNullOrWhiteSpace(this._name)) {
					return "FK_" + this.tableSource.Name + this.tableDestination;
				}
				return _name;
			}
			set {
				_name = value;
			}
		}
		internal void GeneraSql(CSourceWriter sw) {
			sw.WriteLn(string.Format("alter table dbo.{0} add constraint {1} foreign key(", this.tableSource.Name, this.Name));
			sw.AddTab();
			sw.WriteCommaStringList(this.lFieldSource);
			sw.DelTab();
			sw.WriteLn(") references dbo." + this.tableDestination + " (");
			sw.AddTab();
			sw.WriteCommaStringList(this.lFieldDestination);
			sw.DelTab();
			sw.WriteLn(")");
			sw.WriteLn("GO");
		}
		public string GetFieldNamesUpperCaseFirst() {
			StringBuilder sb = new StringBuilder();
			foreach (String Name in this.lFieldSource) {
				sb.Append(SUtil.UpperFirst(Name));
			}
			return sb.ToString();
		}
	}
}
