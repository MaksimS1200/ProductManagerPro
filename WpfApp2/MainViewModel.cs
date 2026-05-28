using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace ProductManagerPro
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;

        public MainViewModel()
        {
            _context = new ApplicationDbContext();
            _context.Database.EnsureCreated();

            Products = new ObservableCollection<Product>();

            LoadCommand = new RelayCommand(_ => LoadProducts());
            AddCommand = new RelayCommand(_ => AddProduct(), _ => CanAddOrUpdate());
            UpdateCommand = new RelayCommand(_ => UpdateProduct(), _ => SelectedProduct != null);
            DeleteCommand = new RelayCommand(_ => DeleteProduct(), _ => SelectedProduct != null);
            ExportCommand = new RelayCommand(_ => ExportToJson(), _ => Products.Count > 0);
        }

        private string _productName = string.Empty;
        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        private decimal _productPrice;
        public decimal ProductPrice
        {
            get => _productPrice;
            set { _productPrice = value; OnPropertyChanged(); }
        }

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (value != null)
                {
                    ProductName = value.Name;
                    ProductPrice = value.Price;
                }
                else
                {
                    ProductName = string.Empty;
                    ProductPrice = 0;
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<Product> Products { get; set; }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExportCommand { get; }

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var product in _context.Products.ToList())
                Products.Add(product);
        }

        private bool CanAddOrUpdate() => !string.IsNullOrWhiteSpace(ProductName) && ProductPrice > 0;

        private void AddProduct()
        {
            var newProduct = new Product { Name = ProductName, Price = ProductPrice };
            _context.Products.Add(newProduct);
            _context.SaveChanges();
            Products.Add(newProduct);
            ClearInputs();
        }

        private void UpdateProduct()
        {
            if (SelectedProduct == null) return;

            SelectedProduct.Name = ProductName;
            SelectedProduct.Price = ProductPrice;
            _context.Entry(SelectedProduct).State = EntityState.Modified;
            _context.SaveChanges();

            int index = Products.IndexOf(SelectedProduct);
            Products[index] = SelectedProduct;

            ClearInputs();
        }

        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            _context.Products.Remove(SelectedProduct);
            _context.SaveChanges();
            Products.Remove(SelectedProduct);
            ClearInputs();
        }

        private void ClearInputs()
        {
            ProductName = string.Empty;
            ProductPrice = 0;
            SelectedProduct = null;
        }

        private void ExportToJson()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"Products_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportData = new
                    {
                        ExportDate = DateTime.Now,
                        TotalProducts = Products.Count,
                        Products = Products.Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Price
                        }).ToList()
                    };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };

                    string jsonString = JsonSerializer.Serialize(exportData, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonString);

                    System.Windows.MessageBox.Show(
                        $"Экспорт выполнен!\n\nФайл: {saveFileDialog.FileName}\nТоваров: {Products.Count}",
                        "Экспорт в JSON",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ошибка: {ex.Message}",
                    "Ошибка экспорта",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}