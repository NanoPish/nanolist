using Windows.UI.Xaml.Controls;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace todolist
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            DatePicker.Date = DateTime.Now;
            Output.ItemsSource = Grab_Entries();
        }
        private class TodoEntry
        {
            public string title { get; set; }
            public string content { get; set; }
            public DateTimeOffset date { get; set; }
            public int doneState { get; set; }
            public SolidColorBrush stateColor { get; set; }
            public int id { get; set; }
        }
        public string error { get; set; }
        private int lastEditId = -1;
        private string sortStr = "";
        private void Save_Edit(object sender, RoutedEventArgs e)
        {
            if (lastEditId == -1)
            {
                ErrorDisplay.Text = "Error: Select an entry to edit before trying to save edits";
                return;
            }
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                using (SqliteCommand updateCommand = new SqliteCommand("UPDATE TodoEntries SET Title_Entry = @TitleEntry, Content_Entry = @ContentEntry, Ticks_Entry = @TicksEntry WHERE Primary_Key = @lastEditId", db))
                {
                    if (Title_Box.Text.Length == 0)
                    {
                        ErrorDisplay.Text = "Error: Title cannot be empty";
                    }
                    else if (Input_Box.Text.Length == 0)
                    {
                        ErrorDisplay.Text = "Error: Content cannot be empty";
                    }
                    else
                    {
                        updateCommand.Parameters.AddWithValue("@TitleEntry", Title_Box.Text);
                        updateCommand.Parameters.AddWithValue("@ContentEntry", Input_Box.Text);
                        updateCommand.Parameters.AddWithValue("@TicksEntry", DatePicker.Date.Value.Ticks);
                        updateCommand.Parameters.AddWithValue("@lastEditId", lastEditId);
                        try
                        {
                            updateCommand.ExecuteNonQuery();
                        }
                        catch (SqliteException error)
                        {
                            ErrorDisplay.Text = "Error: " + error.ToString();
                            return;
                        }
                        Output.ItemsSource = Grab_Entries();
                        Title_Box.Text = "";
                        Input_Box.Text = "";
                        DatePicker.Date = DateTime.Today;
                        lastEditId = -1;
                    }
                }
                db.Close();
            }
        }
        private void Edit_Entry(object sender, RoutedEventArgs e)
        {
            var item = sender as Button;
            string EntryId = item.Tag.ToString();
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                using (SqliteCommand selectCommand = new SqliteCommand("SELECT Title_Entry, Content_Entry, Ticks_Entry from TodoEntries WHERE Primary_Key = @EntryId", db))
                {
                    selectCommand.Parameters.AddWithValue("@EntryId", EntryId);
                    try
                    {
                        using (SqliteDataReader query = selectCommand.ExecuteReader())
                        {
                            query.Read();
                            Title_Box.Text = query.GetString(0);
                            Input_Box.Text = query.GetString(1);
                            DatePicker.Date = new DateTimeOffset(new DateTime(query.GetInt64(2)));
                            lastEditId = int.Parse(EntryId);
                        }
                    }
                    catch (SqliteException error)
                    {
                        ErrorDisplay.Text = "Error: " + error.ToString();
                    }
                }
                db.Close();
            }
        }
        private void Delete_Entry(object sender, RoutedEventArgs e)
        {
            var item = sender as Button;
            string EntryId = item.Tag.ToString();
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                using (SqliteCommand deleteCommand = new SqliteCommand("DELETE FROM TodoEntries WHERE Primary_Key = @EntryId", db))
                {
                    deleteCommand.Parameters.AddWithValue("@EntryId", EntryId);
                    try
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                    catch (SqliteException error)
                    {
                        ErrorDisplay.Text = "Error: " + error.ToString();
                    }
                }
                db.Close();
            }
            Output.ItemsSource = Grab_Entries();
        }
        private void Change_Done_State(object sender, RoutedEventArgs e)
        {
            var item = sender as Button;
            string EntryId = item.Tag.ToString();
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                using (SqliteCommand updateCommand = new SqliteCommand("UPDATE TodoEntries SET Done_State = CASE WHEN Done_State = 1 THEN 0 ELSE 1 END WHERE Primary_Key = @EntryId", db))
                {
                    updateCommand.Parameters.AddWithValue("@EntryId", EntryId);
                    try
                    {
                        updateCommand.ExecuteNonQuery();
                    }
                    catch (SqliteException error)
                    {
                        ErrorDisplay.Text = "Error: " + error.ToString();
                    }
                }
                db.Close();
            }
            Output.ItemsSource = Grab_Entries();
        }
        private void Add_Text(object sender, RoutedEventArgs e)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                using (SqliteCommand insertCommand = new SqliteCommand("INSERT INTO TodoEntries VALUES (NULL, @TitleEntry, @ContentEntry, @TicksEntry, @DoneState)", db))
                {
                    if (Title_Box.Text.Length == 0)
                    {
                        ErrorDisplay.Text = "Error: Title cannot be empty";
                    }
                    else if (Input_Box.Text.Length == 0)
                    {
                        ErrorDisplay.Text = "Error: Content cannot be empty";
                    }
                    else
                    {
                        insertCommand.Parameters.AddWithValue("@TitleEntry", Title_Box.Text);
                        insertCommand.Parameters.AddWithValue("@ContentEntry", Input_Box.Text);
                        insertCommand.Parameters.AddWithValue("@TicksEntry", DatePicker.Date.Value.Ticks);
                        insertCommand.Parameters.AddWithValue("@DoneState", 0);
                        try
                        {
                            insertCommand.ExecuteNonQuery();
                        }
                        catch (SqliteException error)
                        {
                            ErrorDisplay.Text = "Error: " + error.ToString();
                            return;
                        }
                        Output.ItemsSource = Grab_Entries();
                        Title_Box.Text = "";
                        Input_Box.Text = "";
                        DatePicker.Date = DateTime.Today;
                    }
                }
                db.Close();
            }
        }

        private List<TodoEntry> Grab_Entries()
        {
            ErrorDisplay.Text = "";
            List<TodoEntry> entries = new List<TodoEntry>();
            using (SqliteConnection db = new SqliteConnection("Filename=Todo.db"))
            {
                db.Open();
                string constraintStr = "";
                if (doneBox.IsChecked == true)
                    constraintStr = " WHERE Done_State = 0";
                else if (todoBox.IsChecked == true)
                    constraintStr = " WHERE Done_State = 1";
                using (SqliteCommand selectCommand = new SqliteCommand("SELECT Title_Entry, Content_Entry, Ticks_Entry, Done_State, Primary_Key from TodoEntries" + constraintStr + sortStr, db))
                {
                    try
                    {
                        using (SqliteDataReader query = selectCommand.ExecuteReader())
                        {
                            while (query.Read())
                            {
                                TodoEntry tmpEntry = new TodoEntry();
                                tmpEntry.title = query.GetString(0);
                                tmpEntry.content = query.GetString(1);
                                tmpEntry.date = new DateTimeOffset(new DateTime(query.GetInt64(2)));
                                tmpEntry.doneState = query.GetInt16(3);
                                Color color = Microsoft.Toolkit.Uwp.Helpers.ColorHelper.ToColor(tmpEntry.doneState == 1 ? "#33000000" : "#54A81B");
                                tmpEntry.stateColor = new SolidColorBrush(color);
                                tmpEntry.id = query.GetInt32(4);
                                entries.Add(tmpEntry);
                            }
                        }
                    }
                    catch (SqliteException error)
                    {
                        ErrorDisplay.Text = "Error: " + error.ToString();
                        db.Close();
                        return entries;
                    }
                }
                db.Close();
            }
            return entries;
        }
        private void clickBoxDone(object sender, RoutedEventArgs e)
        {
            todoBox.IsChecked = false;
            Output.ItemsSource = Grab_Entries();
        }
        private void clickBoxTodo(object sender, RoutedEventArgs e)
        {
            doneBox.IsChecked = false;
            Output.ItemsSource = Grab_Entries();
        }
        private void SortStandard(object sender, RoutedEventArgs e)
        {
            sortStr = "";
            Output.ItemsSource = Grab_Entries();
        }
        private void SortTitle(object sender, RoutedEventArgs e)
        {
            sortStr = " ORDER BY Title_Entry";
            Output.ItemsSource = Grab_Entries();
        }
        private void SortContent(object sender, RoutedEventArgs e)
        {
            sortStr = " ORDER BY Content_Entry";
            Output.ItemsSource = Grab_Entries();
        }
        private void SortDate(object sender, RoutedEventArgs e)
        {
            sortStr = " ORDER BY Ticks_Entry";
            Output.ItemsSource = Grab_Entries();
        }
    }
}