   M S B A M L   `   `   `  ���� M  IChat.Client.Visual, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null1clr-namespace:Chat.Client.WpfChat.Client.Wpf       Chat.Client.Wpf.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     /localclr-namespace:Chat.Client.Wpf     -     � 
ResizeMode$  NoResize=� � Title$ Windows Update��$	��350��$	��600�����	#FF575757$��1q�$	��600��$	��350��$	��600��$	��350��$��1q� � UseLayoutRounding .  $��Center=�$��Center=� � Icon$ Icons/ChatWindowsIcon.ico��.��� $��1q�.+�� -   $��Options��$	��220��$
��Left=�.+�]� .+���  �� Orientation$ Vertical=�$
��Left=�$��80��$��5,5,5,10q�.+��� $��	Server IP��$��0,0,0,2q����	#FFC7C7C7�� $
��Port��$��0,0,0,2q����	#FFC7C7C7�� $��Client name��$��0,0,0,2q����	#FFC7C7C7�� $ Vertical=�$��Right=�$	��120��$��5,5,5,10q�.+��� -   $��	IpTextBox��$��0.0.0.0��$��Center=�$��26��$��0,0,0,2q�$��0,0,0,1q����	#FF575757���	#FFC7C7C7.���� -   $��PortTextBox��$��0��$��Center=�$��26��$��0,0,0,2q�$��0,0,0,1q����	#FF575757���	#FFC7C7C7.���� -   $��ClientNameTextBox��$��Client��$��Center=�$��26��$��0,0,0,2q�$��0,0,0,1q����	#FF575757���	#FFC7C7C7.���� -   $��ConnectionButton��$��Connect��$��25��$��0,5,0,0q�� -   $
��View��$��	225,0,0,0q�.+�]� .+�]� $	��Top=�.+��� -   $��
HideButton��$��Right=�$��Right=�$��20��$��40��$��5q�.��Hide�� -	   $��OutputRichTextBox��$��5q�$	��225��$	��Top=�$	��Top=�" �� VerticalScrollBarVisibility$
 Auto=� �� 
IsReadOnly . ���	#FF3A3A3A � 	Resources  �� 
TargetType	 �� Property
 �� Value��%3   'J�       �� " J�.��� 	 �@��Margin$
 0q��� -
   $��InputTextBox��$��5,0,5,5q�$	��289�� �� TextWrapping$
 Wrap=�$
 Auto=�$��Center=����	#FF3A3A3A���	#FFC7C7C7 J� 	AllowDrop . �� -   $��
SendButton��$��0,0,5,5q�$��53��$��Right=�.��Send