using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvanCruz.CodeGeneration.SourceGeneration {
	public interface ISourceGenerable {
		void Generate(CSourceWriter sw);
	}

}
