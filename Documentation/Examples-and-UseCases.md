# Examples and Use Cases

This document provides real-world examples and practical use cases for Ludo.Reactive, demonstrating how to solve common programming challenges with reactive patterns.

## Table of Contents

- [Game Development](#game-development)
- [UI Development](#ui-development)
- [Data Processing](#data-processing)
- [State Management](#state-management)
- [Real-time Systems](#real-time-systems)
- [Form Validation](#form-validation)
- [Animation Systems](#animation-systems)

## Game Development

### Player Inventory System

```csharp
using Ludo.Reactive.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string id;
    public string name;
    public int quantity;
    public float weight;
    public int value;
}

public class PlayerInventory : ReactiveMonoBehaviour
{
    [SerializeField] private float maxWeight = 100f;
    [SerializeField] private int maxSlots = 20;

    private ReactiveState<List<InventoryItem>> items;
    private ComputedValue<float> totalWeight;
    private ComputedValue<int> totalValue;
    private ComputedValue<int> usedSlots;
    private ComputedValue<bool> isOverweight;
    private ComputedValue<bool> isFull;
    private ComputedValue<List<InventoryItem>> sortedItems;

    protected override void InitializeReactive()
    {
        items = CreateState(new List<InventoryItem>());

        totalWeight = CreateComputed(builder =>
            builder.Track(items).Sum(item => item.weight * item.quantity));

        totalValue = CreateComputed(builder =>
            builder.Track(items).Sum(item => item.value * item.quantity));

        usedSlots = CreateComputed(builder =>
            builder.Track(items).Count);

        isOverweight = CreateComputed(builder =>
            builder.Track(totalWeight) > maxWeight);

        isFull = CreateComputed(builder =>
            builder.Track(usedSlots) >= maxSlots);

        sortedItems = CreateComputed(builder =>
            builder.Track(items)
                .OrderByDescending(item => item.value)
                .ThenBy(item => item.name)
                .ToList());

        // Effects for game feedback
        CreateEffect(builder =>
        {
            if (builder.Track(isOverweight))
            {
                Debug.Log("Player is overweight! Movement speed reduced.");
                // Reduce player movement speed
            }
        });

        CreateEffect(builder =>
        {
            if (builder.Track(isFull))
            {
                Debug.Log("Inventory is full!");
                // Show UI notification
            }
        });
    }

    public bool TryAddItem(InventoryItem newItem)
    {
        if (isFull.Current) return false;

        items.Update(currentItems =>
        {
            var existingItem = currentItems.FirstOrDefault(item => item.id == newItem.id);
            if (existingItem != null)
            {
                existingItem.quantity += newItem.quantity;
                return new List<InventoryItem>(currentItems);
            }
            else
            {
                var newList = new List<InventoryItem>(currentItems) { newItem };
                return newList;
            }
        });

        return true;
    }

    public bool TryRemoveItem(string itemId, int quantity = 1)
    {
        var success = false;

        items.Update(currentItems =>
        {
            var item = currentItems.FirstOrDefault(i => i.id == itemId);
            if (item != null && item.quantity >= quantity)
            {
                item.quantity -= quantity;
                success = true;

                if (item.quantity <= 0)
                {
                    return currentItems.Where(i => i.id != itemId).ToList();
                }
            }
            return new List<InventoryItem>(currentItems);
        });

        return success;
    }

    // Public read-only access
    public float TotalWeight => totalWeight.Current;
    public int TotalValue => totalValue.Current;
    public int UsedSlots => usedSlots.Current;
    public bool IsOverweight => isOverweight.Current;
    public bool IsFull => isFull.Current;
    public List<InventoryItem> SortedItems => sortedItems.Current;
}
```

### Combat System with Damage Calculation

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class CombatSystem : ReactiveMonoBehaviour
{
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDefense = 5f;

    private ReactiveState<float> attackPower;
    private ReactiveState<float> defense;
    private ReactiveState<float> criticalChance;
    private ReactiveState<float> criticalMultiplier;
    private ReactiveState<bool> isBlocking;
    private ReactiveState<bool> isBerserk;

    private ComputedValue<float> finalAttackPower;
    private ComputedValue<float> finalDefense;
    private ComputedValue<float> damageReduction;

    protected override void InitializeReactive()
    {
        attackPower = CreateState(baseDamage);
        defense = CreateState(baseDefense);
        criticalChance = CreateState(0.1f);
        criticalMultiplier = CreateState(2.0f);
        isBlocking = CreateState(false);
        isBerserk = CreateState(false);

        finalAttackPower = CreateComputed(builder =>
        {
            var base_attack = builder.Track(attackPower);
            var berserk = builder.Track(isBerserk);
            return berserk ? base_attack * 1.5f : base_attack;
        });

        finalDefense = CreateComputed(builder =>
        {
            var base_defense = builder.Track(defense);
            var blocking = builder.Track(isBlocking);
            return blocking ? base_defense * 2f : base_defense;
        });

        damageReduction = CreateComputed(builder =>
        {
            var def = builder.Track(finalDefense);
            return def / (def + 100f); // Diminishing returns formula
        });
    }

    public float CalculateDamage(float incomingDamage)
    {
        var reduction = damageReduction.Current;
        var finalDamage = incomingDamage * (1f - reduction);

        // Check for critical hit
        if (Random.value < criticalChance.Current)
        {
            finalDamage *= criticalMultiplier.Current;
            Debug.Log("Critical hit!");
        }

        return Mathf.Max(1f, finalDamage); // Minimum 1 damage
    }

    public void SetBerserk(bool berserk) => isBerserk.Set(berserk);
    public void SetBlocking(bool blocking) => isBlocking.Set(blocking);
    public void ModifyAttackPower(float modifier) => attackPower.Update(ap => ap + modifier);
    public void ModifyDefense(float modifier) => defense.Update(def => def + modifier);

    // Public read-only access
    public float FinalAttackPower => finalAttackPower.Current;
    public float FinalDefense => finalDefense.Current;
    public float DamageReduction => damageReduction.Current;
}
```

## UI Development

### Dynamic Form with Validation

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class RegistrationForm : ReactiveMonoBehaviour
{
    [SerializeField] private InputField usernameField;
    [SerializeField] private InputField emailField;
    [SerializeField] private InputField passwordField;
    [SerializeField] private InputField confirmPasswordField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text validationMessage;

    private ReactiveState<string> username;
    private ReactiveState<string> email;
    private ReactiveState<string> password;
    private ReactiveState<string> confirmPassword;

    private ComputedValue<bool> isUsernameValid;
    private ComputedValue<bool> isEmailValid;
    private ComputedValue<bool> isPasswordValid;
    private ComputedValue<bool> isPasswordConfirmed;
    private ComputedValue<bool> isFormValid;
    private ComputedValue<string> validationText;

    protected override void InitializeReactive()
    {
        username = CreateState("");
        email = CreateState("");
        password = CreateState("");
        confirmPassword = CreateState("");

        // Validation rules
        isUsernameValid = CreateComputed(builder =>
        {
            var name = builder.Track(username);
            return name.Length >= 3 && name.Length <= 20 &&
                   Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
        });

        isEmailValid = CreateComputed(builder =>
        {
            var mail = builder.Track(email);
            return Regex.IsMatch(mail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        });

        isPasswordValid = CreateComputed(builder =>
        {
            var pass = builder.Track(password);
            return pass.Length >= 8 &&
                   Regex.IsMatch(pass, @"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)");
        });

        isPasswordConfirmed = CreateComputed(builder =>
        {
            var pass = builder.Track(password);
            var confirm = builder.Track(confirmPassword);
            return pass == confirm && pass.Length > 0;
        });

        isFormValid = CreateComputed(builder =>
            builder.Track(isUsernameValid) &&
            builder.Track(isEmailValid) &&
            builder.Track(isPasswordValid) &&
            builder.Track(isPasswordConfirmed));

        validationText = CreateComputed(builder =>
        {
            if (builder.Track(isFormValid))
                return "Form is valid!";

            if (!builder.Track(isUsernameValid))
                return "Username must be 3-20 characters, alphanumeric and underscore only";
            if (!builder.Track(isEmailValid))
                return "Please enter a valid email address";
            if (!builder.Track(isPasswordValid))
                return "Password must be 8+ characters with uppercase, lowercase, and number";
            if (!builder.Track(isPasswordConfirmed))
                return "Passwords do not match";

            return "Please fill out the form";
        });

        // UI binding
        CreateEffect(builder =>
        {
            submitButton.interactable = builder.Track(isFormValid);
        });

        CreateEffect(builder =>
        {
            validationMessage.text = builder.Track(validationText);
            validationMessage.color = builder.Track(isFormValid) ? Color.green : Color.red;
        });

        // Input field listeners
        usernameField.onValueChanged.AddListener(value => username.Set(value));
        emailField.onValueChanged.AddListener(value => email.Set(value));
        passwordField.onValueChanged.AddListener(value => password.Set(value));
        confirmPasswordField.onValueChanged.AddListener(value => confirmPassword.Set(value));

        submitButton.onClick.AddListener(OnSubmit);
    }

    private void OnSubmit()
    {
        if (isFormValid.Current)
        {
            Debug.Log($"Registering user: {username.Current}, {email.Current}");
            // Process registration
        }
    }

    protected override void OnDestroy()
    {
        // Clean up listeners
        usernameField?.onValueChanged.RemoveAllListeners();
        emailField?.onValueChanged.RemoveAllListeners();
        passwordField?.onValueChanged.RemoveAllListeners();
        confirmPasswordField?.onValueChanged.RemoveAllListeners();
        submitButton?.onClick.RemoveAllListeners();

        base.OnDestroy();
    }
}
```

## Data Processing

### Real-time Analytics Dashboard

```csharp
using Ludo.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnalyticsDashboard
{
    private readonly ReactiveState<List<DataPoint>> rawData;
    private readonly ReactiveState<TimeSpan> timeWindow;
    private readonly ReactiveState<string> selectedMetric;

    private readonly ComputedValue<List<DataPoint>> filteredData;
    private readonly ComputedValue<double> average;
    private readonly ComputedValue<double> minimum;
    private readonly ComputedValue<double> maximum;
    private readonly ComputedValue<double> standardDeviation;
    private readonly ComputedValue<List<DataPoint>> trendData;

    public AnalyticsDashboard()
    {
        rawData = ReactiveFlow.CreateState(new List<DataPoint>());
        timeWindow = ReactiveFlow.CreateState(TimeSpan.FromHours(1));
        selectedMetric = ReactiveFlow.CreateState("cpu_usage");

        filteredData = ReactiveFlow.CreateComputed("filteredData", builder =>
        {
            var data = builder.Track(rawData);
            var window = builder.Track(timeWindow);
            var metric = builder.Track(selectedMetric);
            var cutoff = DateTime.Now - window;

            return data
                .Where(d => d.Timestamp >= cutoff && d.Metric == metric)
                .OrderBy(d => d.Timestamp)
                .ToList();
        });

        average = ReactiveFlow.CreateComputed("average", builder =>
        {
            var data = builder.Track(filteredData);
            return data.Count > 0 ? data.Average(d => d.Value) : 0.0;
        });

        minimum = ReactiveFlow.CreateComputed("minimum", builder =>
        {
            var data = builder.Track(filteredData);
            return data.Count > 0 ? data.Min(d => d.Value) : 0.0;
        });

        maximum = ReactiveFlow.CreateComputed("maximum", builder =>
        {
            var data = builder.Track(filteredData);
            return data.Count > 0 ? data.Max(d => d.Value) : 0.0;
        });

        standardDeviation = ReactiveFlow.CreateComputed("stdDev", builder =>
        {
            var data = builder.Track(filteredData);
            var avg = builder.Track(average);

            if (data.Count <= 1) return 0.0;

            var variance = data.Average(d => Math.Pow(d.Value - avg, 2));
            return Math.Sqrt(variance);
        });

        trendData = ReactiveFlow.CreateComputed("trendData", builder =>
        {
            var data = builder.Track(filteredData);
            if (data.Count < 2) return new List<DataPoint>();

            // Simple moving average for trend
            var windowSize = Math.Min(10, data.Count / 2);
            var result = new List<DataPoint>();

            for (int i = windowSize; i < data.Count; i++)
            {
                var window_data = data.Skip(i - windowSize).Take(windowSize);
                var avgValue = window_data.Average(d => d.Value);
                result.Add(new DataPoint
                {
                    Timestamp = data[i].Timestamp,
                    Metric = "trend_" + data[i].Metric,
                    Value = avgValue
                });
            }

            return result;
        });

        // Alert system
        ReactiveFlow.CreateEffect("alertSystem", builder =>
        {
            var avg = builder.Track(average);
            var stdDev = builder.Track(standardDeviation);
            var data = builder.Track(filteredData);

            if (data.Count > 0)
            {
                var latest = data.Last().Value;
                var threshold = avg + (2 * stdDev); // 2 sigma threshold

                if (latest > threshold)
                {
                    Console.WriteLine($"ALERT: {selectedMetric.Current} value {latest:F2} exceeds threshold {threshold:F2}");
                }
            }
        });
    }

    public void AddDataPoint(string metric, double value)
    {
        var dataPoint = new DataPoint
        {
            Timestamp = DateTime.Now,
            Metric = metric,
            Value = value
        };

        rawData.Update(data =>
        {
            var newData = new List<DataPoint>(data) { dataPoint };
            // Keep only last 10000 points to prevent memory issues
            if (newData.Count > 10000)
            {
                newData = newData.Skip(newData.Count - 10000).ToList();
            }
            return newData;
        });
    }

    public void SetTimeWindow(TimeSpan window) => timeWindow.Set(window);
    public void SetSelectedMetric(string metric) => selectedMetric.Set(metric);

    // Public read-only access
    public double Average => average.Current;
    public double Minimum => minimum.Current;
    public double Maximum => maximum.Current;
    public double StandardDeviation => standardDeviation.Current;
    public List<DataPoint> FilteredData => filteredData.Current;
    public List<DataPoint> TrendData => trendData.Current;
}

public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public string Metric { get; set; }
    public double Value { get; set; }
}
```

## State Management

### Shopping Cart System

```csharp
using Ludo.Reactive.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Product
{
    public string id;
    public string name;
    public decimal price;
    public bool isOnSale;
    public decimal salePrice;
    public int stockQuantity;
}

[System.Serializable]
public class CartItem
{
    public Product product;
    public int quantity;
}

public class ShoppingCart : ReactiveMonoBehaviour
{
    private ReactiveState<List<CartItem>> cartItems;
    private ReactiveState<string> discountCode;
    private ReactiveState<decimal> shippingCost;
    private ReactiveState<decimal> taxRate;

    private ComputedValue<decimal> subtotal;
    private ComputedValue<decimal> discountAmount;
    private ComputedValue<decimal> taxAmount;
    private ComputedValue<decimal> total;
    private ComputedValue<int> totalItems;
    private ComputedValue<bool> hasOutOfStockItems;
    private ComputedValue<bool> canCheckout;

    protected override void InitializeReactive()
    {
        cartItems = CreateState(new List<CartItem>());
        discountCode = CreateState("");
        shippingCost = CreateState(5.99m);
        taxRate = CreateState(0.08m); // 8% tax

        subtotal = CreateComputed(builder =>
        {
            var items = builder.Track(cartItems);
            return items.Sum(item =>
            {
                var price = item.product.isOnSale ? item.product.salePrice : item.product.price;
                return price * item.quantity;
            });
        });

        discountAmount = CreateComputed(builder =>
        {
            var code = builder.Track(discountCode);
            var sub = builder.Track(subtotal);

            return code.ToUpper() switch
            {
                "SAVE10" => sub * 0.10m,
                "SAVE20" => sub * 0.20m,
                "FREESHIP" => builder.Track(shippingCost),
                _ => 0m
            };
        });

        taxAmount = CreateComputed(builder =>
        {
            var sub = builder.Track(subtotal);
            var discount = builder.Track(discountAmount);
            var rate = builder.Track(taxRate);
            return (sub - discount) * rate;
        });

        total = CreateComputed(builder =>
        {
            var sub = builder.Track(subtotal);
            var discount = builder.Track(discountAmount);
            var tax = builder.Track(taxAmount);
            var shipping = builder.Track(shippingCost);
            var code = builder.Track(discountCode);

            var shippingCost_final = code.ToUpper() == "FREESHIP" ? 0m : shipping;
            return sub - discount + tax + shippingCost_final;
        });

        totalItems = CreateComputed(builder =>
        {
            var items = builder.Track(cartItems);
            return items.Sum(item => item.quantity);
        });

        hasOutOfStockItems = CreateComputed(builder =>
        {
            var items = builder.Track(cartItems);
            return items.Any(item => item.quantity > item.product.stockQuantity);
        });

        canCheckout = CreateComputed(builder =>
        {
            var items = builder.Track(cartItems);
            var outOfStock = builder.Track(hasOutOfStockItems);
            return items.Count > 0 && !outOfStock;
        });

        // Effects for notifications
        CreateEffect(builder =>
        {
            var outOfStock = builder.Track(hasOutOfStockItems);
            if (outOfStock)
            {
                Debug.Log("Warning: Some items in cart are out of stock!");
            }
        });

        CreateEffect(builder =>
        {
            var itemCount = builder.Track(totalItems);
            Debug.Log($"Cart updated: {itemCount} items");
        });
    }

    public void AddProduct(Product product, int quantity = 1)
    {
        cartItems.Update(items =>
        {
            var existingItem = items.FirstOrDefault(item => item.product.id == product.id);
            if (existingItem != null)
            {
                existingItem.quantity += quantity;
                return new List<CartItem>(items);
            }
            else
            {
                var newItems = new List<CartItem>(items)
                {
                    new CartItem { product = product, quantity = quantity }
                };
                return newItems;
            }
        });
    }

    public void RemoveProduct(string productId)
    {
        cartItems.Update(items =>
            items.Where(item => item.product.id != productId).ToList());
    }

    public void UpdateQuantity(string productId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            RemoveProduct(productId);
            return;
        }

        cartItems.Update(items =>
        {
            var item = items.FirstOrDefault(i => i.product.id == productId);
            if (item != null)
            {
                item.quantity = newQuantity;
            }
            return new List<CartItem>(items);
        });
    }

    public void ApplyDiscountCode(string code) => discountCode.Set(code);
    public void SetShippingCost(decimal cost) => shippingCost.Set(cost);
    public void SetTaxRate(decimal rate) => taxRate.Set(rate);

    public void ClearCart() => cartItems.Set(new List<CartItem>());

    // Public read-only access
    public List<CartItem> Items => cartItems.Current;
    public decimal Subtotal => subtotal.Current;
    public decimal DiscountAmount => discountAmount.Current;
    public decimal TaxAmount => taxAmount.Current;
    public decimal Total => total.Current;
    public int TotalItems => totalItems.Current;
    public bool CanCheckout => canCheckout.Current;
}
```