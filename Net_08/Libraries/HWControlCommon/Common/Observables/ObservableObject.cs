/*
 
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
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

using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Grumpy.DAQFramework.Common

{    /// <summary>
     /// Represents an observable object that provides notifications when properties change.
     /// Implements <see cref="INotifyPropertyChanged"/> to support data binding.
     /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        bool _useExceptions;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableObject"/> class.
        /// </summary>
        /// <param name="useExceptions">If set to <c>false</c>, exceptions will not be thrown when property names are invalid. Default is <c>true</c>/</param>
        public ObservableObject(bool useExceptions = true)
        {
            _useExceptions = useExceptions;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a specified property.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        public void RaisePropertyChanged(string name)
        {
            OnPropertyChanged(name);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for all properties.
        /// </summary>
        public void RaisePropertyChanged()
        {
            PropertyChangedEventHandler pc = PropertyChanged!;

            if (pc != null) {
                pc(this, new PropertyChangedEventArgs(string.Empty));
            }
            return;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a specified property and checks its existence.
        /// Throws an exception if the property does not exist and exceptions are enabled.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <exception cref="ArgumentException">Thrown when the property is not found, and exceptions are enabled.</exception>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            Type tmpType = GetType();
            System.Reflection.PropertyInfo tmpProperty = 
                tmpType.GetProperty(propertyName)!;

            if (tmpProperty != null)
            {
                PropertyChangedEventHandler pc = PropertyChanged!;

                if (pc != null) { 
                    pc(this, new PropertyChangedEventArgs(propertyName));
                }
                return;
            }
            else
            {
                if (_useExceptions) {
                    throw new ArgumentException(
                        $"Property {propertyName} not found.");
                }
            }     
        }

        /// <summary>
        /// Sets the specified field to the given value, raises the <see cref="PropertyChanged"/> event,
        /// and executes a command if the value changes and the command can be executed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="command">The command to execute if the field value changes.</param>
        /// <param name="propertyName">The name of the property. Defaults to the caller member name.</param>
        /// <returns><c>true</c> if the field value changed; otherwise, <c>false</c>.</returns>
        protected bool SetProperty<T>( ref T field, T value, ICommand command,  
                              [CallerMemberName] string propertyName = null!)
        {
            if( SetProperty<T>(ref field, value, propertyName )) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }


        /// <summary>
        /// Sets the specified field to the given value and raises the <see cref="PropertyChanged"/> event 
        /// if the value has changed. Optionally suppresses exceptions based on the class configuration.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="propertyName">
        /// The name of the property associated with the field. Defaults to the caller member name.
        /// </param>
        /// <returns>
        /// <c>true</c> if the field value changed; <c>false</c> if an exception was suppressed or if the 
        /// value did not change.
        /// </returns>
        /// <exception cref="Exception">
        /// Re-throws any exception raised by <see cref="OnPropertyChanged"/> unless exceptions are suppressed 
        /// by the <c>_doNotUseExceptions</c> field.
        /// </exception>
        protected bool SetProperty<T>( ref T field, T value, 
                             [CallerMemberName] string propertyName = null!)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                try {
                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch {
                    
                    if (_useExceptions) {
                        throw;
                    }
                    return false;
                    
                }
            }
            return false;
        }


        /// Sets the specified <see cref="double"/> field to the given value, considering a tolerance level,
        /// raises the <see cref="PropertyChanged"/> event if the value has changed, and executes a command if specified.
        /// </summary>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="command">The command to execute if the field value changes.</param>
        /// <param name="propertyName">
        /// The name of the property associated with the field. Defaults to the caller member name.
        /// </param>
        /// <param name="tolernacePPM">
        /// The tolerance in parts per million (PPM) for determining if the field value has changed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the field value changed and the command was executed; otherwise, <c>false</c>.
        /// </returns>
        protected bool SetProperty(ref double field, 
            double value, 
            ICommand command,
            [CallerMemberName] string propertyName = null!,
            double tolernacePPM = 100)
        {
            if (SetProperty(ref field,value, propertyName, tolernacePPM)) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }


        /// <summary>
        /// Sets the specified <see cref="double"/> field to the given value if the difference exceeds a specified error tolerance,
        /// and raises the <see cref="PropertyChanged"/> event if the value has changed.
        /// </summary>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="propertyName">
        /// The name of the property associated with the field. Defaults to the caller member name.
        /// </param>
        /// <param name="error">
        /// The allowable difference (error tolerance) between the current and new value for the change to be considered significant.
        /// </param>
        /// <returns>
        /// <c>true</c> if the field value changed; <c>false</c> if the change was within the tolerance or if an exception was suppressed.
        /// </returns>
        /// <exception cref="Exception">
        /// Re-throws any exception raised by <see cref="OnPropertyChanged"/> unless exceptions are suppressed 
        /// by the <c>_doNotUseExceptions</c> field.
        /// </exception>

        protected bool SetProperty(ref double field, double value, 
            [CallerMemberName] string propertyName = null!, 
            double error = 1.0e-6 )
        {
            if (Math.Abs(field - value)> Math.Abs(error)) {
                field = value;
                try {
                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch {

                    if (_useExceptions) {
                        throw;        
                    }
                    return false;
                }
            }
            return false;
        }


        /// <summary>
        /// Sets the specified field to the given value and raises the <see cref="PropertyChanged"/> event 
        /// for the property identified by an expression, if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="expr">
        /// An expression identifying the property to be updated, typically provided in the form of a lambda 
        /// expression (e.g., <c>() => PropertyName</c>).
        /// </param>
        /// <returns>
        /// <c>true</c> if the field value changed; <c>false</c> if the value did not change or if an exception was suppressed.
        /// </returns>
        /// <exception cref="Exception">
        /// Re-throws any exception raised by <see cref="OnPropertyChanged"/> unless exceptions are suppressed 
        /// by the <c>_doNotUseExceptions</c> field.
        /// </exception>
        protected bool SetProperty<T>(ref T field, T value, 
            Expression<Func<T>> expr)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                var lambda = expr as LambdaExpression;
                MemberExpression memberExpression;

                if (lambda.Body is UnaryExpression) {

                    var unaryExpr = (UnaryExpression)lambda.Body;
                    memberExpression = (MemberExpression)unaryExpr.Operand;
                }
                else {
                    memberExpression = (MemberExpression)lambda.Body;
                }

                try {
                    OnPropertyChanged(memberExpression.Member.Name);
                    return true;
                }
                catch {
                    
                    if (_useExceptions) {
                        throw;
                    }
                    return false; ;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the specified <see cref="double"/> field to the given value if it differs by more than a specified error tolerance,
        /// or if the current field value is <c>NaN</c>. Raises the <see cref="PropertyChanged"/> event for the property 
        /// identified by an expression if the value has changed.
        /// </summary>
        /// <param name="field">The field to be set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        /// <param name="expr">
        /// An expression identifying the property to be updated, typically provided in the form of a lambda 
        /// expression (e.g., <c>() => PropertyName</c>).
        /// </param>
        /// <param name="error">
        /// The allowable difference (error tolerance) between the current and new value for the change to be considered significant.
        /// Defaults to <c>1.0e-9</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the field value changed; <c>false</c> if the change was within the tolerance or if an exception was suppressed.
        /// </returns>
        /// <exception cref="Exception">
        /// Re-throws any exception raised by <see cref="OnPropertyChanged"/> unless exceptions are suppressed 
        /// by the <c>_doNotUseExceptions</c> field.
        /// </exception>
        protected bool SetProperty(ref double field, double value, 
            Expression<Func<double>> expr, double error = 1.0e-9)
        {
            if ( double.IsNaN(field) ||  
                (Math.Abs(field - value) > Math.Abs(error)) ) {

                field = value;
                var lambda = expr as LambdaExpression;
                MemberExpression memberExpression;

                if (lambda.Body is UnaryExpression) {

                    var unaryExpr = (UnaryExpression)lambda.Body;
                    memberExpression = (MemberExpression)unaryExpr.Operand;
                }
                else {
                    memberExpression = (MemberExpression)lambda.Body;
                }

                try {
                    OnPropertyChanged(memberExpression.Member.Name);
                    return true;
                }
                catch  {

                    if (_useExceptions) {
                       throw;
                    }
                    return false; ;
                }
            }
            return false;
        }


        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the property identified by an expression.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="expr">
        /// An expression identifying the property for which to raise the <see cref="PropertyChanged"/> event,
        /// typically provided in the form of a lambda expression (e.g., <c>() => PropertyName</c>).
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the expression does not correctly reference a property name.
        /// </exception>
        protected void RaisePropertyChanged<T>(Expression<Func<T>> expr)
        {
            var lambda = expr as LambdaExpression;
            MemberExpression memberExpression;

            if (lambda.Body is UnaryExpression) {

                var unaryExpr = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpr.Operand;
            }
            else {
                memberExpression = (MemberExpression)lambda.Body;
            }

             OnPropertyChanged(memberExpression.Member.Name);

        }
    }
}
