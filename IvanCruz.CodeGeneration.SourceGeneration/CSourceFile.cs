using System.IO;
using IvanCruz.Util;

namespace IvanCruz.CodeGeneration.SourceGeneration {
    public class CDirectory : IStorableInDirectory, IGenerable {
        public CDirectory ParentDirectory { get; set; }
        public readonly CActualList<IStorableInDirectory> lChildren = new CActualList<IStorableInDirectory>();
        private CDirectory _ActualDirectory;
        public CDirectory ActualDirectory {
            get {
                if(this._ActualDirectory == null) {
                    return this;
                }
                return this._ActualDirectory;
            }
        }
        private CSourceFile _ActualFile;
        public CSourceFile ActualFile {
            get {
                return this._ActualFile;
            }
        }

        public string Name { get; private set; }
        public CDirectory(string defaultDirectoryPath) {
            this.Name = defaultDirectoryPath;
        }
        public CDirectory(CDirectory parent, string defaultDirectoryPath) : this(defaultDirectoryPath) {
            this.ParentDirectory = parent;
        }
        public CDirectory AddDirectory(string name) {
            return (CDirectory)this.lChildren.AddR(this._ActualDirectory = new CDirectory(this, name));
        }
        public IStorableInDirectory AddElement(IStorableInDirectory element) {
            element.ParentDirectory = this;
            if (element is CSourceFile) {
                this._ActualFile = (CSourceFile)element;
            }
            return this.lChildren.AddR(element);
        }
        public string FullName {
            get {
                if (ParentDirectory != null) {
                    return Path.Combine(ParentDirectory.FullName, this.Name);
                }
                return this.Name;
            }
        }
        public void Generate(TextWriter mensajes) {
            this.Generate(mensajes, true);
        }
        public void Generate(TextWriter mensajes, bool GenerarAncestros) {
            //Generar Ancestros
            if (GenerarAncestros) {
                if (this.ParentDirectory != null) {
                    this.ParentDirectory.Generate(mensajes, true);
                }
            }
            if (!Directory.Exists(this.FullName)) {
                Directory.CreateDirectory(this.FullName);
                mensajes.WriteLine("Directorio creado: " + this.FullName);
            }
            //Generar Hijos
            foreach (IStorableInDirectory item in this.lChildren) {
                if (item is CDirectory) {
                    ((CDirectory)item).Generate(mensajes, false);
                } else {
                    item.Generate(mensajes);
                }
            }
        }
    }
    public interface IStorableInDirectory : IGenerable {
        CDirectory ParentDirectory { get; set; }
    }
    public abstract class CSourceFile : IGenerable, IStorableInDirectory {
        public CDirectory ParentDirectory { get; set; }
        public abstract void Generate(TextWriter lMensaje);
    }
}
