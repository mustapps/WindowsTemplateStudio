﻿Это универсальная версия шаблона MVVM.  [Шаблон Model-View-ViewModel](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel) можно использовать на всех платформах XAML. Он предназначен для четкого разделения вопросов, связанных с элементами пользовательского интерфейса и их логикой.

В шаблоне MVVM есть три основных компонента: модель, представление и модель представления. Каждая имеет свое особое предназначение.

MVVM Basic не является платформой (framework), но предлагает необходимый минимум функций для создания приложения на базе шаблона Model-View-ViewModel (MVVM).
Используйте это решение, если вы не можете или не хотите использовать сторонний Framework для MVVM.

MVVM Basic не является заменой полнофункциональному Framework для MVVM и не включает некоторые возможности других framework'ов. Наиболее очевидные из них: навигация в первую очередь по ViewModel, IOC и обмен сообщениями. Если вам нужны эти функции, выберите framework, который их поддерживает.

Проекты, созданные с помощью MVVM Basic, включают два важных класса: Observable и RelayCommand.
**Observable** содержит реализацию интерфейса "NotifyPropertyChanged" и используется как базовый класс для всех моделей представлений. Это позволяет легко обновлять привязанные свойства в представлении.
**RelayCommand** содержит реализацию интерфейса "ICommand", позволяющую легко распространить команды вызова представления на модель представления, не обрабатывая события пользовательского интерфейса напрямую.
