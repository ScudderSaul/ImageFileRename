using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageFileRename
{
    public class PngObservable : ObservableCollection<string>
    {
      

       public PngObservable(List<string> pngFiles)
        {
            foreach (string st in pngFiles)
            {
               Items.Add(st); 
            }
        }

        public void Add(string st)
        {
            Items.Add(st);
        }
    }

    public class JpgObservable : ObservableCollection<string>
    {
        public JpgObservable(List<string> jpgFiles)
        {
            foreach (string st in jpgFiles)
            {
                Items.Add(st);
            }
        }

        public void Add(string st)
        {
            Items.Add(st);
        }
    }

}
