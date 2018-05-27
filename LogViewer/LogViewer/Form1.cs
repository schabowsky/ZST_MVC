using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogViewer
{
    public partial class Form1 : Form
    {
        private DataTable dataTable = new DataTable();
        private string type = null;
        public Form1()
        {
            InitializeComponent();
            comboBox1.DataSource = GetAllTables();
        }

        private void Form1_Load_2(object sender, EventArgs e)
        {
        }

        private void showButton_Click(object sender, EventArgs e)
        {
            type = comboBox1.Text;
            dataGridView2.DataSource = GetData(type);
            dataGridView2.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);
            comboBox2.DataSource = GetAllColumns();
        }

        private DataTable GetData(string type)
        {
            DataTable tempTable = new DataTable();
            string connString = "Data Source=.\\SQLEXPRESS;Initial Catalog=logs;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand command = new SqlCommand($"SELECT * FROM {type}", conn))
                {
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    tempTable.Load(reader);
                }
            }
            return tempTable;
        }

        private DataTable FilterData(string phrase, string column, string type)
        {
            DataTable tempTable = new DataTable();
            string connString = "Data Source=.\\SQLEXPRESS;Initial Catalog=logs;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand command = new SqlCommand($"SELECT * FROM {type} WHERE {column} LIKE '%{phrase}%'", conn))
                {
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    tempTable.Load(reader);
                }
            }
            return tempTable;
        }
        private string[] GetAllTables()
        {
            List<string> result = new List<string>();
            string connString = "Data Source=.\\SQLEXPRESS;Initial Catalog=logs;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand command = new SqlCommand("SELECT name FROM sys.Tables", conn))
                {
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                        result.Add(reader["name"].ToString());
                }
            }
            return result.ToArray();
        }

        private string[] GetAllColumns()
        {
            List<string> columns = new List<string>();
            foreach (DataGridViewTextBoxColumn column in dataGridView2.Columns)
            {
                columns.Add(column.Name);
            }
            return columns.ToArray();
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            string phrase = textBox2.Text;
            string column = comboBox2.Text.ToUpper();
            if (phrase != null || column != null || type != null)
            {
                dataGridView2.DataSource = FilterData(phrase, column, type);
                dataGridView2.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);
            }
        }

        private void DeleteSelectedRow( DataGridViewRow row)
        {
            string connString = "Data Source=.\\SQLEXPRESS;Initial Catalog=logs;Integrated Security=True";
            List<string> cells = new List<string>();

            foreach (DataGridViewTextBoxCell cell in row.Cells)
            {
                cells.Add(cell.Value.ToString());
            }

            string deleteString = $"DELETE FROM {type} WHERE :conditions:;";
            string[] conditions = new string[cells.Count];
            conditions[0] = "Date=@0";
            conditions[1] = "Source_ID=@1";
            conditions[2] = "Text_Column_1=@2";

            for (int iterator = 2; iterator < cells.Count - 1; iterator++)
            {
                conditions[iterator + 1] = $"Text_Column_{iterator}=@{iterator + 1}";
            }

            string conditionsName = string.Join(" AND ", conditions);
            deleteString = deleteString.Replace(":conditions:", conditionsName);

            using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand deleteCommand = new SqlCommand(deleteString, conn))
                    {
                        deleteCommand.Parameters.Add(new SqlParameter("0", DateTime.Parse(cells[0])));
                        deleteCommand.Parameters.Add(new SqlParameter("1", cells[1]));
                        deleteCommand.Parameters.Add(new SqlParameter("2", cells[2]));
                        for (int iterator = 3; iterator < cells.Count; iterator++)
                            deleteCommand.Parameters.Add(new SqlParameter($"{iterator}", cells[iterator]));

                        conn.Open();
                        deleteCommand.ExecuteNonQuery();
                    }
                }
        }

        private void comboBox2_OnDropDownOpened(object sender, System.EventArgs e)
        {
            comboBox2.DataSource = GetAllColumns();
        }

        private void comboBox1_OnDropDownOpened(object sender, System.EventArgs e)
        {
            comboBox1.DataSource = GetAllTables();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = dataGridView2.CurrentRow;
            DeleteSelectedRow(row);
            dataGridView2.DataSource = GetData(type);
            dataGridView2.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);
            comboBox2.DataSource = GetAllColumns();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
