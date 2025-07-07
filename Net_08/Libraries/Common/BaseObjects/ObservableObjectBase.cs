/*
Copyright (c) 2025 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Grumpy.Common.BaseObjects
{
    /// <summary>
    /// A base class for observable objects that implements the <see cref="INotifyPropertyChanged"/> interface.
    /// Provides functionality for property change notifications and error handling.
    /// Can be configured to suppress exceptions during property operations.
    /// In all cases the last error descrption is stored in the <see cref="LastError"/> property.
    /// </summary>
    public class ObservableObjectBase : INotifyPropertyChanged
    {

        /// <summary>
        /// Default minimum difference required to consider the value changed (tolerance)
        /// <summary>
        private const double _DefaultThreshold = 1.0e-6;

        private bool _noExceptions;
        private readonly object _lastErrorLock = new object();
        private string _lastError;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableObjectBase"/> class.
        /// </summary>
        /// <param name="noExceptions">
        /// If set to <c>true</c>, suppresses exceptions thrown when property change notifications fail.
        /// If <c>false</c> (default), exceptions will be thrown for invalid property names.
        /// </param>
        public ObservableObjectBase(bool noExceptions = false) {

            _noExceptions = noExceptions;
            _lastError = string.Empty;
        }

        /// <summary>
        /// Gets or sets the last error message encountered during property operations.
        /// </summary>
        public string LastError {
            get {
                lock (_lastErrorLock) {

                    return (string)_lastError.Clone();
                }
            }
            protected set {

                lock (_lastErrorLock) {

                    _lastError = string.IsNullOrEmpty(value)? string.Empty : (string)value.Clone();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _ClearLastError() {
            lock (_lastErrorLock) {
                _lastError = string.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetPropertyNameFromExpression(LambdaExpression expr) {
            MemberExpression memberExpression;
            if (expr.Body is UnaryExpression unaryExpr)
                memberExpression = (MemberExpression)unaryExpr.Operand;
            else
                memberExpression = (MemberExpression)expr.Body;
            return memberExpression.Member.Name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandlePropertyException(ArgumentException ex) {
            LastError = ex.Message;
            return _noExceptions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValidProperty(string propertyName) {
            return GetType().GetProperty(propertyName) != null;
        }



        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a specific property.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        public void RaisePropertyChanged(string name) {
            OnPropertyChanged(name);
        }

        public void RaisePropertyChanged() {

            PropertyChangedEventHandler? pc = PropertyChanged;

            if (pc != null) {

                pc(this, new PropertyChangedEventArgs(string.Empty));
            }
            return;
        }


        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a specific property.
        /// Validates the property name before raising the event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName) {

            _ClearLastError();

            if (IsValidProperty(propertyName)) {

                PropertyChangedEventHandler pc = PropertyChanged!;

                if (pc != null) {

                    pc(this, new PropertyChangedEventArgs(propertyName));
                }
                return;
            }
            else {

                 LastError = $"Property {propertyName} not found.";

                if (!_noExceptions) {

                    throw new ArgumentException(LastError);
                }
            }
        }

        /// <summary>
        /// Sets the value of a property and raises the <see cref="PropertyChanged"/> event.
        /// Executes a command if the property value changes.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="command">The command to execute if the property value changes.</param>
        /// <param name="propertyName">The name of the property. Automatically provided by the compiler.</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field,
            T value,
            ICommand command,
            [CallerMemberName] string propertyName = null!) {

            if (SetProperty<T>(ref field, value, propertyName)) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the value of a property and raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="propertyName">The name of the property. Automatically provided by the compiler.</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field,
            T value,
            [CallerMemberName] string propertyName = null!) {

            _ClearLastError();

            if (!EqualityComparer<T>.Default.Equals(field, value)) {

                field = value;

                try {

                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch (ArgumentException ex) {

                    if (HandlePropertyException(ex))
                        return false;
                    throw;
                }
            }
            return false;
        }


        /// <summary>
        /// Sets the value of a property and raises the <see cref="PropertyChanged"/> event.
        /// Designed to work with double properties, allowing for a tolerance level in the comparison.
        /// Executes a command if the property value changes.
        /// </summary>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value of the property (double).</param>
        /// <param name="command">The command to execute if the property value changes.</param>
        /// <param name="propertyName">The name of the property. Automatically provided by the compiler.</param>
        /// <param name="threshold">Property change threshold (double) Defined minimum value change to trigger an update.</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty(ref double field,
            double value,
            ICommand command,
            [CallerMemberName] string? propertyName = null,
            double threshold = _DefaultThreshold) {

            if (SetProperty(ref field, value, propertyName!, threshold)) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }


        /// <summary>
        /// Sets the value of a property and raises the <see cref="PropertyChanged"/> event.
        ///  Designed to work with double properties, allowing for a tolerance level in the comparison.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="propertyName">The name of the property. Automatically provided by the compiler.</param>
        /// <param name="threshold">The name of the property. Defines minimum value change to trigger an update.</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty(ref double field,
            double value,
            [CallerMemberName] string? propertyName = null,
            double threshould = _DefaultThreshold) {

            _ClearLastError();

            if (Math.Abs(field - value) > Math.Abs(threshould)) {

                field = value;

                try {

                    OnPropertyChanged(propertyName!);
                    return true;
                }
                catch (ArgumentException ex) {
                    if (HandlePropertyException(ex))
                        return false;
                    throw;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the value of a property and raises the <see cref="PropertyChanged"/> event
        /// for the property specified by a lambda expression.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value to assign to the property.</param>
        /// <param name="expr">
        /// A lambda expression representing the property (e.g., <c>() => PropertyName</c>).
        /// The property name is extracted from this expression for notification.
        /// </param>
        /// <returns>
        /// True if the property value was changed and the event was raised; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This overload allows specifying the property to notify using a lambda expression,
        /// which is safer than using a string literal for the property name.
        /// </remarks>
        protected bool SetProperty<T>(ref T field,
            T value,
            Expression<Func<T>> expr) {

            _ClearLastError();

            if (!EqualityComparer<T>.Default.Equals(field, value)) {

                field = value;

                var propertyName = GetPropertyNameFromExpression(expr);

                try {

                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch (ArgumentException ex) {

                    if (HandlePropertyException(ex))
                        return false;
                    throw;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the value of a double property and raises the <see cref="PropertyChanged"/> event
        /// for the property specified by a lambda expression.
        /// </summary>
        /// <param name="field">The backing field of the property.</param>
        /// <param name="value">The new value to assign to the property.</param>
        /// <param name="expr">
        /// A lambda expression representing the property (e.g., <c>() => PropertyName</c>).
        /// The property name is extracted from this expression for notification.
        /// </param>
        /// <param name="threshold">
        /// The minimum difference required to consider the value changed (tolerance).
        /// Default is defined by _DefaultThreshold constatnt.
        /// </param>
        /// <returns>
        /// True if the property value was changed and the event was raised; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This overload allows specifying the property to notify using a lambda expression,
        /// which is safer than using a string literal for the property name. The value is
        /// considered changed if it is NaN or the absolute difference exceeds the specified tolerance.
        /// </remarks>
        protected bool SetProperty(ref double field, double value,
            Expression<Func<double>> expr, double threshold = _DefaultThreshold) {

            _ClearLastError();

            if (double.IsNaN(field) ||
                (Math.Abs(field - value) > Math.Abs(threshold))) {

                field = value;
                var propertyName = GetPropertyNameFromExpression(expr);

                try {

                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch (ArgumentException ex) {

                    if (HandlePropertyException(ex))
                        return false;
                    throw;
                }
            }
            return false;
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> expr) {

            var lambda = expr as LambdaExpression;
            MemberExpression memberExpression;

            _ClearLastError();

            if (lambda.Body is UnaryExpression) {

                var unaryExpr = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpr.Operand;
            }
            else {

                memberExpression = (MemberExpression)lambda.Body;
            }

            try {
                
                OnPropertyChanged(memberExpression.Member.Name);
            }
            catch (ArgumentException ex) {

                if (HandlePropertyException(ex))
                    return;
                throw;
            }
        }
    }
}
