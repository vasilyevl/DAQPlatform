using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Windows.Input;

namespace Grumpy.Common
{
    public class ObservableObject : INotifyPropertyChanged
    {
        bool _doNotUseExceptions;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableObject(bool noExceptions = false)
        {
            _doNotUseExceptions = noExceptions;
        }

        public void RaisePropertyChanged(string name)
        {
            OnPropertyChanged(name);
        }

        public void RaisePropertyChanged()
        {
            PropertyChangedEventHandler pc = PropertyChanged!;

            if (pc != null) {
                pc(this, new PropertyChangedEventArgs(string.Empty));
            }
            return;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Type tmpType = GetType();
            System.Reflection.PropertyInfo tmpProperty = tmpType.GetProperty(propertyName)!;

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
                if (!_doNotUseExceptions) {
                    throw new ArgumentException($"Property {propertyName} not found.");
                }
            }     
        }

        protected bool SetProperty<T>(ref T field, T value, ICommand command,  [CallerMemberName] string propertyName = null!)
        {
            if( SetProperty<T>(ref field, value, propertyName )) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                try {
                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch {
                    
                    if (_doNotUseExceptions) { 
                        return false;
                    }
                    throw;
                }
            }
            return false;
        }

        protected bool SetProperty(ref double field, double value, ICommand command,
            [CallerMemberName] string propertyName = null!, double tolernacePPM = 100)
        {
            if (SetProperty(ref field,value, propertyName, tolernacePPM)) {

                if (command.CanExecute(field)) {

                    command.Execute(field);
                }
                return true;
            }

            return false;
        }

        protected bool SetProperty(ref double field, double value, 
            [CallerMemberName] string propertyName = null!, double error = 1.0e-6 )
        {
            if (Math.Abs(field - value)> Math.Abs(error)) {
                field = value;
                try {
                    OnPropertyChanged(propertyName);
                    return true;
                }
                catch {

                    if (_doNotUseExceptions) {
                        
                        return false;
                    }
                    throw;
                }
            }
            return false;
        }

        protected bool SetProperty<T>(ref T field, T value, Expression<Func<T>> expr)
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
                    
                    if (_doNotUseExceptions) {
                        return false;
                    }
                    throw;
                }
            }
            return false;
        }

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

                    if (_doNotUseExceptions) {
                        return false;
                    }
                    throw ;
                }
            }
            return false;
        }
 
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
