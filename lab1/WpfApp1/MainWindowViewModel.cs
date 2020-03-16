using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string inputText;
        //private bool exactly;
        //private bool partTitle;
        private ObservableCollection<Movie> searchCollection = new ObservableCollection<Movie>();
        public string InputText
        {
            get { return inputText; }
            set
            {
                inputText = value;
                OnPropertyChanged();
            }
        }
        //public bool Exactly
        //{
        //    get => exactly;
        //    set
        //    {
        //        exactly = value;
        //        OnPropertyChanged();
        //    }
        //}
        //public bool PartTitle
        //{
        //    get => partTitle;
        //    set
        //    {
        //        partTitle = value;
        //        OnPropertyChanged();
        //    }
        //}
        public ObservableCollection<Movie> SearchCollection
        {
            get { return searchCollection; }
            set
            {
                searchCollection = value;
                OnPropertyChanged();
            }
        }
        private NpgsqlConnection connection = new NpgsqlConnection("Server=91.245.227.59;Port=5432;User ID=Searcher;Password=qwerty;Database=SearchingMethods;");
        public ICommand Find { get; }
        public ICommand Clear { get; }
        public MainWindowViewModel()
        {
            connection.Open();
            Find = new Command(arg => SearchResults());
            Clear = new Command(arg => SearchCollection.Clear());
        }
        ~MainWindowViewModel()
        {
            connection.Close();
        }
        public void SearchResults()
        {
            if (string.IsNullOrEmpty(InputText))
            {
                MessageBox.Show("Text was null or empty. Check input string.");
                return;
            }
            SearchCollection.Clear();
            var words = InputText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var sqlCommands = new List<NpgsqlCommand>();
            while(words.Count>0)
            {
                sqlCommands.Add(new NpgsqlCommand(SqlGeneration(words),connection));
                if(words.Count>0) words.RemoveRange(words.Count - 1, 1);
            }
            //sqlCommands.Add(new NpgsqlCommand(TitleGenerationSql(InputText),connection));
            sqlCommands.Add(new NpgsqlCommand(PartialGenerateSql(InputText), connection));            
            AppendingResults(sqlCommands);
        }
        private void AppendingResults(List<NpgsqlCommand> searchs)
        {
            var list = new List<Movie>();
            foreach (var e in searchs)
            {
                var reader = e.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var m = new Movie(reader.GetValue(0).ToString(), Convert.ToInt32(reader.GetValue(1)));
                        if (list.FirstOrDefault(x => x.Name == m.Name && x.Year == m.Year) == null)
                            list.Add(m);
                    }
                }
                reader.Close();
            }
            if (list.Count() > 0)
                list.ForEach(x => { if (SearchCollection.Count == 10) return; SearchCollection.Add(x); });
            else MessageBox.Show("There wasn't any strings");
        }
        private string SqlGeneration(List<string> tmp)
        {
            var builder = new StringBuilder();
            builder.Append("SELECT name, year FROM public.movies WHERE");
            if (tmp.Last().All(char.IsDigit) && tmp.Last().Count() == 4)
            {
                builder.Append($" year = {tmp.Last()} AND");
                tmp.RemoveAt(tmp.Count-1);
            }
            foreach (var e in tmp)
            {
                builder.Append($" LOWER(name) LIKE \'%{e}%\' AND");
            }
            var sql = builder.ToString();
            return sql.Remove(sql.Length - 4, 4) + " LIMIT 10;";
        }
        private string TitleGenerationSql(string str)
        {
            var tmp = str.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            builder.Append("SELECT name, year FROM public.movies WHERE");
            var year = "";
            if (tmp.Last().All(char.IsDigit) && tmp.Last().Count() == 4)
            {
                year = tmp.Last();
                builder.Append($" year = {tmp.Last()} AND");
            }                        
            var title = string.IsNullOrEmpty(year) ? str : str.Remove(str.Length - 4, 4);                  
            if(title.Count() > 0 && title.Last() == ' ') title = title.Remove(title.Length - 1, 1);
            builder.Append($" LOWER(name) LIKE \'%{title.ToLower()}%\';");
            return builder.ToString();
        }

        private string PartialGenerateSql(string str)
        {
            var tmp = str.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var lst = new List<string>();
            var years = new List<string>();
            var builder = new StringBuilder();
            builder.Append("SELECT name, year FROM public.movies WHERE");
            foreach (var t in tmp)
            {
                var chars = t.ToCharArray();
                if (t.All(char.IsDigit))
                {
                    if (t.Count() == 4)
                        years.Add(t);
                    lst.Add($" name LIKE \'%{t}%\' OR");
                }
                else lst.Add($" LOWER(name) LIKE \'%{t.ToLower()}%\' OR");
            }
            if (years.Count() > 0)
            {
                for (int i = 0; i < years.Count; i++)
                {
                    if (i == 0)
                        builder.Append(" (");
                    if (i == years.Count - 1)
                    {
                        builder.Append($" year = {years[i]}) AND (");
                    }
                    else builder.Append($" year = {years[i]} OR");
                }
            }
            lst.ForEach(x => builder.Append(x));
            var sql = builder.ToString();
            if (sql.EndsWith("OR"))
                sql = sql.Remove(sql.Length - 3);
            else sql = sql.Remove(sql.Length - 6);
            return sql + (years.Count > 0 && lst.Count > 0 ? ")" : "") + " LIMIT 10;";
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public Movie(string name, int year)
        {
            Name = name;
            Year = year;
        }
        public Movie(string str)
        {
            var tmp = str.Split(',');
            var tmp1 = tmp[1].Split('(');
            Id = Convert.ToInt32(tmp[0]);
            Name = tmp1[0].EndsWith(" ") ? tmp1[0].Substring(0, tmp1[0].Length - 1) : tmp1[0];
            Year = tmp1.Length > 1 ? Convert.ToInt32(tmp1[1].Substring(0, 4)) : -1;

        }
    }
}
