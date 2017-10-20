using System.IO;

namespace IvanCruz.CodeGeneration.SourceGeneration {
	public interface IGenerable {
		void Generate(TextWriter lMensaje);
	}
}
