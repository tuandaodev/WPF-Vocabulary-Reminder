import React, { useContext } from 'react';
import { Layout, Text, Icon, IconProps } from '@ui-kitten/components';
import { styles } from '../styles/styles';
import { Image, Linking } from 'react-native';
import { AppContext } from '../App';

import 'react-native-gesture-handler';

import { createStackNavigator } from '@react-navigation/stack';
import { TouchableWithoutFeedback } from 'react-native-gesture-handler';

export const FlexiIcon = ( settingsIconProps: IconProps ) => (
    <Icon { ...settingsIconProps } width={ 22 } height={ 22 } fill='#333' />
);

const Stack = createStackNavigator();

export const InfoView = () => {
    return (
        <Layout style={ styles.stackNavigatorWrapper } >
            <Stack.Navigator
                screenOptions={ {
                    cardStyle: { backgroundColor: '#fff' }
                } }
            >
                <Stack.Screen
                    name='infoMainView'
                    component={ InfoMainView }
                    options={ {
                        title: '',
                        headerStyle: {
                            shadowColor: 'transparent',
                            elevation: 0
                        }
                    } }
                />
            </Stack.Navigator>
        </Layout>
    );
};


const InfoMainView = ( props: any ) => { // TODO: types

    const appData = useContext( AppContext );

    const { increaseTapsCount, logoutAccount, userInfo } = appData;

    const { navigation } = props;

    return (
        <>
            <Layout
                style={ styles.centeredElement }
            >
                <TouchableWithoutFeedback onPress={ increaseTapsCount }>
                    <Image
                        source={ require( './../img/icon.png' ) }
                        style={ styles.iconImage }
                    />
                </TouchableWithoutFeedback>

                <Text style={ [ styles.text, styles.boldText, styles.biggerText, styles.textWithTopMargin ] }>
                    Vocabulary Reminder
                </Text>

                <Text style={ [ styles.text, styles.smallerText ] }>
                Fastest way to learn new words
                </Text>
            </Layout>


            <Layout
                style={ styles.verticalSpacer }
            />
            

            <Layout
                style={ styles.infoContainer }
            >
                <Layout style={ styles.infoColTwo }>
                    <Text style={ [ styles.text, styles.boldText, styles.biggerText ] }>Hi, { userInfo?.userName } </Text>
                </Layout>
            </Layout>

            <Layout
                style={ styles.verticalSpacer }
            />
            
            <Layout
                style={ styles.infoContainer }
            >
                <Layout style={ styles.infoColTwo }>
                    <Text
                        style={ [ styles.text, styles.boldText, styles.biggerText ] }
                        onPress={ () => logoutAccount() }
                    >
                        Logout
                    </Text>
                </Layout>

                <Layout style={ styles.infoColThree } />
            </Layout>

            <Layout
                style={ styles.backupDivider }
            />

            <Layout style={ styles.versionBox } >
                <Text style={ [ styles.text, styles.boldText, styles.leftAlignedText, styles.biggerText ] }>1.0</Text>
                <Text
                    style={ [ styles.text, styles.leftAlignedText, styles.smallerText ] }>
                        App version
                </Text>
            </Layout>
        </>
    );
};
