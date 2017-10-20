using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvanCruz.CodeGeneration.CSharpGeneration {
	public abstract class CControlDefBase {
		public string Name;
		public string Parent;
		public int x;
		public int y;
		public abstract CField Declare();
		public abstract List<CSentence>Init();
		public abstract List<CSentence> FormToItem();
		public abstract List<CSentence> ItemToForm(); 

	}
	public class CTextBoxDef:CControlDefBase {
		public int Width;
		public string Value;
		public int MaxLenght;
		public override CField Declare() {
			throw new NotImplementedException();
		}
		public override List<CSentence> Init() {
			throw new NotImplementedException();
		}
		public override List<CSentence> FormToItem() {
			throw new NotImplementedException();
		}
		public override List<CSentence> ItemToForm() {
			throw new NotImplementedException();
		}
	}
	public class CCheckBoxDef : CControlDefBase {
		public bool Value;
		public override CField Declare() {
			throw new NotImplementedException();
		}
		public override List<CSentence> FormToItem() {
			throw new NotImplementedException();
		}
		public override List<CSentence> Init() {
			throw new NotImplementedException();
		}
		public override List<CSentence> ItemToForm() {
			throw new NotImplementedException();
		}
	}
}
