   M S B A M L   `   `   `  ����M  IVocabularyReminder, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:VocabularyReminderVocabularyReminder  5   	   #    VocabularyReminder.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:VocabularyReminder     5   	   U QMaterialDesignThemes.Wpf, Version=3.0.1.920, Culture=neutral, PublicKeyToken=nullFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   -   5          �Name$  Frm_MainWindow��5   	      MaterialDesignBody#���   5	   	   $��Regular"�5
   	   $��13(�5   	   &   System.Windows.Media.TextOptions   TextFormattingMode$ Ideal=�5   	      TextRenderingMode$
 Auto=�5   	     MaterialDesignPaper#���  5   	     MaterialDesignFont#���  5   	    � Title$ Vocabulary Reminder��5   	   $��746.25��6%   $	��995��65   .��5      �  � RowDefinitions 5   
   �� 5      $��1* �6   �� 5      $��2* �6   �� 5      $��1* �6   �� 5      $��2* �6   �� 5      $��1* �6   �� 5      $��2* �6   �� 5      $��1* �6   �� 5      $��2* �6   �� 5      $��1* �6   �� 5      $
��0.6* �6    � ColumnDefinitions 5   
   �� 5      $
��0.5* �6   6.   �� 5       $��2* �6   6,   �� 5!      $��1* �6   6,   �� 5"      $��2* �6   6,   �� 5#      $��1* �6   6,   �� 5$      $��2* �6   6,   �� 5%      $��1* �6   6,   �� 5&      $��2* �6   �� 5'      $
��0.5* �6   .+�5*   
   �� -   5         ��Name$ Inp_ListWord��5*       �� TextWrapping$
 Wrap=�6(   $��3��6<   $��1��6L   $��5��6Y    �� AcceptsReturn . $��3��6   6�   �� 5+   
   -   5        	 ��Name$	 
Btn_Import��5+      $��Import��6%   $��1��66   $��1��6_   $
��Auto��6l   �� 5,   
   -   5        $	 Btn_ProcessDeleteData��5,      $��Delete All Data��60   $��7��6J   $��5��6Z   $
��Auto��6�   �� 5-   
   -   5        $	 Btn_StartLearning��5-      $��Start Learning��6,   $��3��6E   $��7��6U   $��3��6�   $
��Auto��6�   �� 5/   
   $��9��6   $��10��6!   .@�50      � -   5        
 �Name$
 Status_ProgessBar��50      $	��100��65    � Value$ 0Q�6A    � Maximum$ 0Q�6K   $��15��6W   �� 51      -   5         ��Name$ Status_Message��51      $
��Wrap=�60   $��
Message...��6D   52      �� 53   
   -   5        $ Inp_TimeRepeat��53      $��1��6*   $
��Left=�6:   $��22��6U   $��	128,4,0,0q�6a   $��7��6t   $
 Wrap=�6�   $	��120��6�   $	��Top=�6�   $��37��6�    �� TextAlignment$ Right=�6�   �� 54   
   $��Time/word (s):��6   $
��Left=�6)   $��7��6D   $	��Top=�6Q   $��0,4,0,0q�6i   $��1��6z   �� 55   
   -	   5        $	 Btn_PreloadMp3��55      $��Download Mp3��6)   $��1��6@   $��5��6P   $
��Auto��6z   �� 56   
   -
   5         ��Name$ Inp_RandomOption��56      $��Random Words��6-   $��1��6D   $
��Left=�6T   $��	0,55,0,26q�6o   $��7��6�    d� 	IsChecked$
 TrueY�6�   $��15��6�   $	��148��6�   $��2��6�   �� 57   
   -   5        $	 Btn_Import_Auto��57      $��Import 3000 words��6*   $��7��6F   $��3��6V   $
��Auto��6�   �� 58   
   $��Remembered Words:��6   $��7��6,   $
��Left=�6<   $��1��6W   $	��Top=�6d   �� 59   
   $��Imported Words:��6   $��7��6*   $
��Left=�6:   $��1��6U   $	��Top=�6b   $��0,31,0,0q�6z   $	��113��6�   �� 5:   
   -   5         ��Name$! Label_Stats_RememberedWords��5:      $��0��65   $��7��6A   $
��Left=�6Q   $��1��6l   $	��Top=�6y   $��49��6�   $��	150,0,0,0q�6�   $��2��6�   �� 5;   
   -   5        $ Label_Stats_ImportedWords��5;      $��0��63   $��7��6?   $
��Left=�6O   $��1��6j   $	��Top=�6w   $��
150,31,0,0q�6�   $��49��6�   $��2��6�   �� 5<   
   -   5        $	 Btn_ShowLearnedList��5<      $��Show Learned Words��6.   $��7��6K   $��7��6[   $
��Auto��6�   5=      5>      