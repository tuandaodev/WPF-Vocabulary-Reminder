   M S B A M L   `   `   `  ����M  IVocabularyReminder, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:VocabularyReminderVocabularyReminder  5   	   %    VocabularyReminder.RegisterForm   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:VocabularyReminder     5   	     � Title$  Register New Account��5   	   $	��438��6&   $��469.622��63    � WindowStartupLocation$ CenterScreen=�6C   .��5	      � 
�������$��0,0,2,0q�6    � ColumnDefinitions 5
   
   �� 5      $
��115* �6   �� 5      $
��248* �6   �� 5      $	��97* �6   .+�5   
   �� -   5        $��textBlockHeading��5   N   $��23��6   $
��Left=�6    $��	31,36,0,0q�6;   $��Register New Account��6f   $	��Top=�6�   $	��194��6�   $��17(�6�   $��ExtraCondensed&�6�   $��1��6�   � 5   
   $��
10,0,10,40q�6   
�������$	��283��66   $��Bottom=�6C   $��3��6^    � RowDefinitions 5      �� 5      $
��252* �6    .+�5      �� -   5        $��textBlockFirstname��5   R   $��23��6   $
��Left=�6$   $��	55,43,0,0q�6?   $��	Username:��6l   $	��Top=�6}   $	��110��6�   �� 5      -   5        $��textBlockEmailId��5   R   $��23��6   $
��Left=�6$   $��	55,76,0,0q�6?   $��Email��6j   $	��Top=�6w   $	��110��6�   �� 5      -   5        $��textBlockPassword��5   S   $��23��6   $
��Left=�6$   $��
55,109,0,0q�6?   $��	Password:��6l   $	��Top=�6}   $	��110��6�   �� 5      -   5        $��textBlockConfirmPwd��5   S   $��23��6   $
��Left=�6$   $��
55,144,0,0q�6?   $��Confirm Password:��6n   $	��Top=�6�   $	��110��6�   �� 5      -   5         ��Name$ inp_Username��5   Q   $��23��6   $
��Left=�6"   $��
168,35,0,0q�6=   $	��Top=�6g   $	��204��6   �� 5      -   5        $ 	inp_Email��5   Q   $��23��6   $
��Left=�6"   $��
168,68,0,0q�6=   $	��Top=�6d   $	��204��6|   G� 5      -   5         G�Name$ inp_Password��5   V   $��23��6   $
��Left=�6&   $��168,101,0,0q�6A   $	��Top=�6l   $	��204��6�   G� 5      -	   5        $ inp_PasswordConfirm��5   V   $��23��6   $
��Left=�6&   $��168,135,0,0q�6A   $	��Top=�6s   $	��204��6�   �� 5       -
   5         ��Name$ 	btnSubmit��5    a   $��Submit��6   $��26��6&   $
��Left=�62   $��
43,217,0,0q�6M   $	��Top=�6t   $��80��6�   6�   �� 5!      -   5        $ 	btn_Reset��5!   a   $��Reset��6   $��26��6%   $
��Left=�61   $��168,217,0,0q�6L   $	��Top=�6t   $��80��6�   6�   �� 5"      -   5        $ 
btn_Cancel��5"   b   $��Cancel��6   $��26��6&   $��Right=�62   $��
0,217,68,0q�6N   $	��Top=�6v   $��80��6�   6�   �� 5#      -   5        $ 	btn_Login��5#   `   $��Login��6   $��26��6%   $��Right=�61   $��	0,-46,2,0q�6M   $	��Top=�6s   $��80��6�   6�   �� 5$      -   5         ��Name$ labelErrorMessage��5$   S   $��23��6   $
��Left=�6$   $��
55,176,0,0q�6?   $	��Top=�6n   $	��317��6�   ���	#FFFF36366�   5%      5&      5'      