using IvanCruz.CodeGeneration.CSharpGeneration;
using IvanCruz.CodeGeneration.LibDB1;
using System;
using System.IO;
using System.Data;
using IvanCruz.Util;
using IvanCruz.CodeGeneration.SourceGeneration;
using System.Text;

namespace IvanCruz.CodeGeneration.SqlServerCSharp {
	public class CGenerator {
		public void GeneratePocos(CDBM db, CSharpProject p, CUsing NamespaceName, TextWriter mensajes) {
			foreach (CTableM t in db.lTable) {
				p.AddCSharpFile(PocoName(t.Name), NamespaceName);
				CClass c = p.AddClass(PocoName(t.Name));
				c.lUsing.Add(CSharpStandarUsing.System);
				CConstructor consVacio = new CConstructor(c, CSharpVisibility.cvPublic);
				CConstructor cons = new CConstructor(c, CSharpVisibility.cvPublic);
				//Se añaden al final por estética de la clase generada
				foreach (CFieldM f in t.lFieldAll) {
					CType aux = CGenerator.SqlServerTD2CSharp(f.Td, f.IsNullable);
					if (aux.Equals(CSharpPrimitiveType.cDateTime)) {
						c.lUsing.Add(CSharpStandarUsing.System);
					}
					c.AddField(new CField(f.Name, aux, CSharpVisibility.cvPublic));
					cons.AddParam(SUtil.LowerFirst(f.Name), CGenerator.SqlServerTD2CSharp(f.Td, f.IsNullable));
					cons.AddSentence(new CTextualSentence($"this.{f.Name} = {SUtil.LowerFirst(f.Name)};"));
				}
				c.AddConstructor(consVacio);
				c.AddConstructor(cons);
			}
			mensajes.WriteLine("Pocos generados en: " + p.RootDirectory.ActualDirectory.FullName);
		}

		private string PocoName(string tableName) {
			return "C" + tableName + "Poco";
		}

		public void GenerateProject(CDBM db, string dir, string NamespaceName, string projectName, TextWriter mensajes) {
			CSharpProject p = new CSharpProject(projectName, dir);
			CDirectory baseDir = p.ActualDirectory;
			CUsing Namespace = new CUsing(NamespaceName);
			GenerateDef(db, p, NamespaceName, mensajes);
			p.AddChildDirectory(baseDir, "Poco");
			CUsing NamespacePocos = new CUsing(NamespaceName + ".Poco");
			GeneratePocos(db, p, NamespacePocos, mensajes);
			p.AddChildDirectory(baseDir, "Dao");
			GenerateDaos(db, p, NamespaceName + ".Dao", NamespacePocos, mensajes);
			CUsing NamespaceDao = new CUsing(NamespaceName + ".Dao");
			p.AddChildDirectory(baseDir, "Bo");
			GenerateBo(db, p, NamespaceName + ".Bo", NamespacePocos, NamespaceDao, mensajes);
			p.Generate(mensajes);
		}

		private void GenerateDef(CDBM db, CSharpProject p, string namespaceName, TextWriter mensajes) {
			string className = DefName(db.Name);
			p.AddCSharpFile(className + ".generated", namespaceName);
			CClass c = p.AddClass(className, isStatic: true);
			c.lUsing.Add(CSharpStandarUsing.System);
			c.lUsing.Add(new CUsing("IvanCruz.Util.UtilBD"));
			foreach (CTableM t in db.lTable) {
				c.AddField(new CField(t.Name, new TextualType(ClassDefName(t.Name)),CSharpVisibility.cvPublic,isStatic:true,InitialValue:" new " + ClassDefName(t.Name) + "()"));

				//CConstructor consVacio = new CConstructor(c, CSharpVisibility.cvPublic);
				//CConstructor cons = new CConstructor(c, CSharpVisibility.cvPublic);
				////Se añaden al final por estética de la clase generada
				//foreach (CFieldM f in t.lFieldAll) {
				//	CType aux = CGenerator.SqlServerTD2CSharp(f.Td, f.IsNullable);
				//	if (aux.Equals(CSharpPrimitiveType.cDateTime)) {
				//		c.lUsing.Add(CSharpStandarUsing.System);
				//	}
				//	c.AddField(new CField(f.Name, aux, CSharpVisibility.cvPublic));
				//	cons.AddParam(SUtil.LowerFirst(f.Name), CGenerator.SqlServerTD2CSharp(f.Td, f.IsNullable));
				//	cons.AddSentence(new CTextualSentence($"this.{f.Name} = {SUtil.LowerFirst(f.Name)};"));
				//}
				//c.AddConstructor(consVacio);
				//c.AddConstructor(cons);
				CClass cTable = p.AddClass(ClassDefName(t.Name), isStatic: false);
				cTable.ParentClassName = "CTableDef";
				CProperty prop;
				prop = new CProperty("TableName", CSharpVisibility.cvPublic, new TextualType("string"), hasGet: true,hasSet:false);
				prop.Override = true;
				prop.lSentenceGet.Add(new CTextualSentence("return " + SUtil.DoubleQuote(t.Name) + ";"));
				cTable.AddProperty(prop);
				prop = new CProperty("SingularTitle", CSharpVisibility.cvPublic, new TextualType("string"), hasGet: true,hasSet:false);
				prop.Override = true;
				prop.lSentenceGet.Add(new CTextualSentence("return " + SUtil.DoubleQuote(t.SingularTitle) + ";"));
				cTable.AddProperty(prop);
				CField auxField;
				StringBuilder sb = new StringBuilder();
				prop = new CProperty("Fields", CSharpVisibility.cvPublic, new TextualType("System.Collections.Generic.IEnumerable<CFieldDef>"), hasGet: true,hasSet:false);
				prop.Override = true;
				foreach(CFieldM f in t.lFieldAll) {
					auxField = new CField(f.Name, new TextualType("CFieldDef"), CSharpVisibility.cvPublic, isStatic: false);
					sb.Clear();
					sb.Append("new CFieldDef");
					sb.Append("(");
					sb.Append(SUtil.DoubleQuote(f.Name));
					if (!string.IsNullOrWhiteSpace(f.Title)) {
						sb.Append(",");
						sb.Append(SUtil.DoubleQuote(f.Title));
					}
					sb.Append(")");
					auxField.InitialValue = sb.ToString();
					cTable.AddField(auxField);
					prop.lSentenceGet.Add(new CTextualSentence("yield return " + f.Name + ";"));
				}
				cTable.AddProperty(prop);


		//public override System.Collections.Generic.IEnumerable<CFieldDef> Fields {
		//	get {
		//		yield return Id;
		//		yield return CodigoMedico;
		//		yield return Nomb;
		//	}
		//}
			}
			mensajes.WriteLine("Definición generada en: " + p.RootDirectory.ActualDirectory.FullName);
		}

		private string ClassDefName(string name) {
			return "C" + name + "Def"; 
		}

		private string DefName(string name) {
			return "S" + name + "Def";
		}

		private void GenerateBo(CDBM db, CSharpProject p, string NamespaceName, CUsing NamespacePocos, CUsing NamespaceDao, TextWriter mensajes) {
			string ClassName = "SBo";
			p.AddCSharpFile(ClassName + ".generated", NamespaceName);
			CClass c = p.AddClass(ClassName, isStatic: true, isPartial: true);
			c.lUsing.Add(CSharpStandarUsing.System_Collections_Generic);
			c.lUsing.Add(CSharpStandarUsing.System_Data_SqlClient);
			c.lUsing.Add(NamespacePocos);
			c.lUsing.Add(NamespaceDao);
			c.lUsing.Add(new CUsing("IvanCruz.Util.UtilBD"));
			CMethod m;
			foreach (CTableM t in db.lTable) {
				//Metodo getTableAll
				m = c.AddMethod(new CMethod($"get{t.Name}All", new TextualType($"List <C{t.Name}Poco>"), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddSentence(new CTextualSentence($"return S{t.Name}Dao.SelectAll(con);"));
				//Metodo TableInsert
				m = c.AddMethod(new CMethod($"{t.Name}Insert", new TextualType("void"), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType($"C{t.Name}Poco"));
				m.AddSentence(new CTextualSentence($"S{t.Name}Dao.Insert(con,item);"));
				//Metodo TableUpdate
				m = c.AddMethod(new CMethod($"{t.Name}Update", new TextualType("void"), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType($"C{t.Name}Poco"));
				m.AddSentence(new CTextualSentence($"S{t.Name}Dao.Update(con,item);"));
				//Metodo TableDelete
				m = c.AddMethod(new CMethod($"{t.Name}Delete", new TextualType("void"), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType($"C{t.Name}Poco"));
				StringBuilder sb = new StringBuilder();
				sb.Append("No se ha podido eliminar ");
				if (t.Gender == EGender.male) {
					sb.Append("el ");
				} else {
					sb.Append("la ");
				}
				sb.Append(t.SingularTitle);
				sb.Append(" porque hay otras tablas que se referencian a ");
				if (t.Gender == EGender.male) {
					sb.Append("él");
				} else {
					sb.Append("ella");
				}
				m.AddSentence(new CTextualSentence("try {"));
				m.AddSentence(new CTextualSentence($"\tS{t.Name}Dao.Delete(con,item);"));

				m.AddSentence(new CTextualSentence("} catch (SqlException sex) {"));
				m.AddSentence(new CTextualSentence("\tswitch (sex.Number) {"));
				m.AddSentence(new CTextualSentence("\tcase 547://Error por clave foranea apuntando a registro a eliminar"));
				m.AddSentence(new CTextualSentence($"\t\tthrow new BoException({SUtil.DoubleQuote(sb.ToString())},sex);"));
				m.AddSentence(new CTextualSentence("default:throw;"));
				m.AddSentence(new CTextualSentence("\t}"));
				m.AddSentence(new CTextualSentence("}"));
			}
		}

		private void GenerateDaos(CDBM db, CSharpProject p, string NamespaceName, CUsing usingModel, TextWriter mensajes) {
			foreach (CTableM t in db.lTable) {
				string ClassName = "S" + t.Name + "Dao";
				p.AddCSharpFile(ClassName + ".generated", NamespaceName);

				CClass c = p.AddClass(ClassName, isStatic: true);
				c.lUsing.Add(CSharpStandarUsing.System);
				c.IsPartial = true;
				CMethod m;

				//CreateFromReader
				m = c.AddMethod(new CMethod("CreateFromReader", new TextualType(PocoName(t.Name)), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("dr", new TextualType("SqlDataReader"));
				byte i = 0;
				m.AddSentence(new CTextualSentence($"{PocoName(t.Name)} result = new {PocoName(t.Name)}();"));
				foreach (CFieldM f in t.lFieldAll) {
					string textoDataReaderGet = SqlServerGetDataReaderGet(f.Td, i);
					if (!f.IsNullable) {
						m.AddSentence(new CTextualSentence($"result.{f.Name} = {textoDataReaderGet};"));
					} else {
						m.AddSentence(new CTextualSentence($"if (dr.IsDBNull({i})) {{"));
						m.AddSentence(new CTextualSentence($"\tresult.{f.Name} = null;"));
						m.AddSentence(new CTextualSentence("}else{"));
						m.AddSentence(new CTextualSentence($"\tresult.{f.Name} = {textoDataReaderGet};"));
						m.AddSentence(new CTextualSentence("}"));
					}
					i++;
				}
				m.AddSentence(new CTextualSentence("return result;"));

				//SelectAll
				m = c.AddMethod(new CMethod("SelectAll", new TextualType("List<" + PocoName(t.Name) + ">"), CSharpVisibility.cvPublic, isStatic: true));
				m.AddParam("con", new TextualType("SqlConnection"));
				c.lUsing.Add(CSharpStandarUsing.System_Data_SqlClient);

				m.AddSentence(new CTextualSentence($"List<{PocoName(t.Name)}> result = new List<{PocoName(t.Name)}>();"));
				c.lUsing.Add(CSharpStandarUsing.System_Collections_Generic);
				c.lUsing.Add(usingModel);
				m.AddSentence(new CTextualSentence("using (SqlCommand com = con.CreateCommand()) {"));
				m.AddSentence(new CTextualSentence($"\tcom.CommandText = {SUtil.DoubleQuote(t.GeneraSelectAll())};"));
				m.AddSentence(new CTextualSentence("\tusing (SqlDataReader dr = com.ExecuteReader()){"));
				m.AddSentence(new CTextualSentence("\t\twhile(dr.Read()){"));
				m.AddSentence(new CTextualSentence($"\t\t\tresult.Add({ClassName}.CreateFromReader(dr));"));
				m.AddSentence(new CTextualSentence("\t\t}"));
				m.AddSentence(new CTextualSentence("\t}"));
				m.AddSentence(new CTextualSentence("}"));
				m.AddSentence(new CTextualSentence("return result;"));

				//SelectByPk
				if (t.lFieldData.Count > 0) { //Si hay algo que seleccionar. Si no hay campos de datos no tendría sentido seleccionar por clave primaria.
					m = c.AddMethod(new CMethod("SelectByPk", new TextualType(PocoName(t.Name)), CSharpVisibility.cvPublic, isStatic: true));
					m.AddParam("con", new TextualType("SqlConnection"));
					foreach (CFieldM f in t.lFieldPk) {
						m.AddParam(f.Name, CGenerator.SqlServerTD2CSharp(f.Td, f.IsNullable));
					}
					m.AddSentence(new CTextualSentence(PocoName(t.Name) + " result = null;"));
					c.lUsing.Add(usingModel);
					m.AddSentence(new CTextualSentence("using (SqlCommand com = con.CreateCommand()) {"));
					m.AddSentence(new CTextualSentence($"\tcom.CommandText = {SUtil.DoubleQuote(t.GeneraSelectByPk())};"));
					foreach (CFieldM f in t.lFieldPk) {
						m.AddSentence(new CTextualSentence("\tcom.Parameters.AddWithValue(" + SUtil.DoubleQuote(SUtilBD.ParamName(f.Name)) + ", " + f.Name + ");"));
					}
					m.AddSentence(new CTextualSentence("\tusing (SqlDataReader dr = com.ExecuteReader()){"));
					m.AddSentence(new CTextualSentence("\t\tif(dr.Read()){"));
					m.AddSentence(new CTextualSentence($"\t\t\tresult = {ClassName}.CreateFromReader(dr);"));
					m.AddSentence(new CTextualSentence("\t\t}"));
					m.AddSentence(new CTextualSentence("\t}"));
					m.AddSentence(new CTextualSentence("}"));
					m.AddSentence(new CTextualSentence("return result;"));
				}
				//Insert
				m = c.AddMethod(new CMethod("Insert", CSharpPrimitiveType.cVoid, CSharpVisibility.cvPublic, true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType(PocoName(t.Name)));
				m.AddSentence(new CTextualSentence("using(SqlCommand com = con.CreateCommand()){"));
				m.AddSentence(new CTextualSentence($"\tcom.CommandText = {SUtil.DoubleQuote(t.GeneraInsert())};"));
				foreach (CFieldM f in t.lFieldAll) {
					if (!f.IsIdentity) {
						if (!f.IsNullable) {
							m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},item.{f.Name});"));
						} else {
							m.AddSentence(new CTextualSentence($"if (item.{f.Name} == null) {{"));
							m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},DBNull.Value);"));
							m.AddSentence(new CTextualSentence("}else{"));
							m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},item.{f.Name});"));
							m.AddSentence(new CTextualSentence("}"));
						}
					}
				}
				m.AddSentence(new CTextualSentence("\tcom.ExecuteNonQuery();"));
				foreach (CFieldM f in t.lFieldAll) {
					if (f.IsIdentity) {
						string ConvertTo;
						switch (f.Td) {
							case SqlDbType.BigInt:
								ConvertTo = "(long)";
								break;
							case SqlDbType.Int:
								ConvertTo = "(int)";
								break;
							case SqlDbType.SmallInt:
								ConvertTo = "(short)";
								break;
							case SqlDbType.TinyInt:
								ConvertTo = "(byte)";
								break;
							default:
								throw new Exception("Tipo de dato no considerado en Identity");
						}
						m.AddSentence(new CTextualSentence($"\titem.{f.Name} = {ConvertTo}SUtilBD.getIdentity(con);"));
						c.lUsing.Add(new CUsing("IvanCruz.Util"));

					}
				}

				m.AddSentence(new CTextualSentence("}"));
				//Update
				m = c.AddMethod(new CMethod("Update", CSharpPrimitiveType.cVoid, CSharpVisibility.cvPublic, true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType(PocoName(t.Name)));
				m.AddSentence(new CTextualSentence("using(SqlCommand com = con.CreateCommand()){"));
				m.AddSentence(new CTextualSentence($"\tcom.CommandText = {SUtil.DoubleQuote(t.GeneraUpdate())};"));
				foreach (CFieldM f in t.lFieldData) {
					AddParameterInUpdate(m, f);
				}
				foreach (CFieldM f in t.lFieldPk) {
					AddParameterInUpdate(m, f);
				}
				m.AddSentence(new CTextualSentence("\tcom.ExecuteNonQuery();"));
				m.AddSentence(new CTextualSentence("}"));
				//Delete
				m = c.AddMethod(new CMethod("Delete", CSharpPrimitiveType.cVoid, CSharpVisibility.cvPublic, true));
				m.AddParam("con", new TextualType("SqlConnection"));
				m.AddParam("item", new TextualType(PocoName(t.Name)));
				m.AddSentence(new CTextualSentence("using(SqlCommand com = con.CreateCommand()){"));
				m.AddSentence(new CTextualSentence($"\tcom.CommandText = {SUtil.DoubleQuote(t.GeneraDelete())};"));
				foreach (CFieldM f in t.lFieldPk) {
					m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},item.{f.Name});"));
				}
				m.AddSentence(new CTextualSentence("\tcom.ExecuteNonQuery();"));
				m.AddSentence(new CTextualSentence("}"));
			}
			mensajes.WriteLine("Pocos generados en: " + p.RootDirectory.ActualDirectory.FullName);
		}

		private static void AddParameterInUpdate(CMethod m, CFieldM f) {
			if (!f.IsNullable) {
				m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},item.{f.Name});"));
			} else {
				m.AddSentence(new CTextualSentence($"if (item.{f.Name} == null) {{"));
				m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},DBNull.Value);"));
				m.AddSentence(new CTextualSentence("}else{"));
				m.AddSentence(new CTextualSentence($"\tcom.Parameters.AddWithValue({ SUtil.DoubleQuote(f.ParameterName)},item.{f.Name});"));
				m.AddSentence(new CTextualSentence("}"));
			}
		}
		private static string SqlServerGetDataReaderGet(SqlDbType sqlType, short order) {
			switch (sqlType) {
				case System.Data.SqlDbType.BigInt:
					break;
				case System.Data.SqlDbType.Binary:
					break;
				case System.Data.SqlDbType.Bit:
					return "dr.GetBoolean(" + order.ToString() + ")";
				case System.Data.SqlDbType.Char:
					break;
				case System.Data.SqlDbType.DateTime:
					return "dr.GetDateTime(" + order.ToString() + ")";
				case System.Data.SqlDbType.Decimal:
					break;
				case System.Data.SqlDbType.Float:
					//Sí es raro que un Sql Float sea un C# Double pero es así.https://msdn.microsoft.com/en-us/library/system.data.sqldbtype.aspx#Mtps_DropDownFilterText
					return "dr.GetDouble(" + order.ToString() + ")";
				case System.Data.SqlDbType.Image:
					break;
				case System.Data.SqlDbType.Int:
					return "dr.GetInt32(" + order.ToString() + ")";
				case System.Data.SqlDbType.Money:
					break;
				case System.Data.SqlDbType.NChar:
					break;
				case System.Data.SqlDbType.NText:
					break;
				case System.Data.SqlDbType.NVarChar:
					break;
				case System.Data.SqlDbType.Real:
					break;
				case System.Data.SqlDbType.UniqueIdentifier:
					break;
				case System.Data.SqlDbType.SmallDateTime:
					break;
				case System.Data.SqlDbType.SmallInt:
					return "dr.GetInt16(" + order.ToString() + ")";
				case System.Data.SqlDbType.SmallMoney:
					break;
				case System.Data.SqlDbType.Text:
					return "dr.GetString(" + order.ToString() + ")";
				case System.Data.SqlDbType.Timestamp:
					break;
				case System.Data.SqlDbType.TinyInt:
					return "dr.GetByte(" + order.ToString() + ")";
				case System.Data.SqlDbType.VarBinary:
					return "dr.GetSqlBytes(" + order.ToString() + ").Value";
				case System.Data.SqlDbType.VarChar:
					return "dr.GetString(" + order.ToString() + ")";
				case System.Data.SqlDbType.Variant:
					break;
				case System.Data.SqlDbType.Xml:
					break;
				case System.Data.SqlDbType.Udt:
					break;
				case System.Data.SqlDbType.Structured:
					break;
				case System.Data.SqlDbType.Date:
					return "dr.GetDateTime(" + order.ToString() + ")";
				case System.Data.SqlDbType.Time:
					break;
				case System.Data.SqlDbType.DateTime2:
					break;
				case System.Data.SqlDbType.DateTimeOffset:
					break;
				default:
					break;
			}
			throw new Exception("Tipo de dato Sql no considerado:" + sqlType.ToString());
		}

		public static CType SqlServerTD2CSharp(SqlDbType sqlType, bool isNullable) {
			switch (sqlType) {
				case System.Data.SqlDbType.BigInt:
					break;
				case System.Data.SqlDbType.Binary:
					break;
				case System.Data.SqlDbType.Bit:
					if (isNullable) {
						return CSharpPrimitiveType.cBoolNullable;
					} else {
						return CSharpPrimitiveType.cBool;
					}
				case System.Data.SqlDbType.Char:
					break;
				case System.Data.SqlDbType.DateTime:
					if (isNullable) {
						return CSharpPrimitiveType.cDateTimeNullable;
					} else {
						return CSharpPrimitiveType.cDateTime;
					}
				case System.Data.SqlDbType.Decimal:
					break;
				case System.Data.SqlDbType.Float:
					//sí es raro que de Float de sql pase a double de C# pero https://msdn.microsoft.com/en-us/library/system.data.sqldbtype.aspx#Mtps_DropDownFilterText
					if (isNullable) {
						return CSharpPrimitiveType.cDoubleNullable;
					} else {
						return CSharpPrimitiveType.cDouble;
					}
				case System.Data.SqlDbType.Image:
					break;
				case System.Data.SqlDbType.Int:
					if (isNullable) {
						return CSharpPrimitiveType.cIntNullable;
					} else {
						return CSharpPrimitiveType.cInt;
					}
				case System.Data.SqlDbType.Money:
					break;
				case System.Data.SqlDbType.NChar:
					break;
				case System.Data.SqlDbType.NText:
					break;
				case System.Data.SqlDbType.NVarChar:
					break;
				case System.Data.SqlDbType.Real:
					break;
				case System.Data.SqlDbType.UniqueIdentifier:
					break;
				case System.Data.SqlDbType.SmallDateTime:
					break;
				case System.Data.SqlDbType.SmallInt:
					if (isNullable) {
						return CSharpPrimitiveType.cShortNullable;
					} else {
						return CSharpPrimitiveType.cShort;
					}
				case System.Data.SqlDbType.SmallMoney:
					break;
				case System.Data.SqlDbType.Text:
					return CSharpPrimitiveType.cString;
				case System.Data.SqlDbType.Timestamp:
					break;
				case System.Data.SqlDbType.TinyInt:
					if (isNullable) {
						return CSharpPrimitiveType.cByteNullable;
					} else {
						return CSharpPrimitiveType.cByte;
					}
				case System.Data.SqlDbType.VarBinary:
					return CSharpPrimitiveType.cArrayBytes;
				case System.Data.SqlDbType.VarChar:
					return CSharpPrimitiveType.cString;
				case System.Data.SqlDbType.Variant:
					break;
				case System.Data.SqlDbType.Xml:
					break;
				case System.Data.SqlDbType.Udt:
					break;
				case System.Data.SqlDbType.Structured:
					break;
				case System.Data.SqlDbType.Date:
					if (isNullable) {
						return CSharpPrimitiveType.cDateNullable;
					} else {
						return CSharpPrimitiveType.cDate;
					}
				case System.Data.SqlDbType.Time:
					break;
				case System.Data.SqlDbType.DateTime2:
					break;
				case System.Data.SqlDbType.DateTimeOffset:
					break;
				default:
					break;
			}
			throw new Exception("Tipo de dato Sql no considerado:" + sqlType.ToString());
		}
	}
}
