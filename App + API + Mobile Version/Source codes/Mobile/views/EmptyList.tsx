import React, { useContext } from 'react';
import { Layout, Text, Button } from '@ui-kitten/components';
import { styles } from '../styles/styles';
import { Image, View } from 'react-native';
import { AppContext } from '../App';
import { DEMO_WORDS } from '../utils/demoData';


export const EmptyList = () => {

    const appData = useContext( AppContext );
    const { storeData, getData } = appData;

    return (
        <>
            <Layout style={ styles.walletInstructionsWrapper }>
                <Layout style={ styles.walletInstructions }>
                    <Text style={ [ styles.text, styles.titleText ] } category='h4'>Hello there! 👋</Text>

                    <Text style={ [ styles.text, styles.instructionsText ] }>
                        This is your wallet view. All the words that you add in your wallet will show up here.
                    </Text>

                    <Text style={ [ styles.text, styles.instructionsText ] }>
                        There is no vocabulary here. Please go to your PC and add new word to let us can prepare your wallet. Then click "Refresh Data"
                    </Text>

                    <View
                        style={ styles.centeredSimpleView }
                    >
                        <Image
                            source={ require( './../img/start-page.jpg' ) }
                            resizeMode={ 'contain' }
                            // eslint-disable-next-line react-native/no-inline-styles
                            style={ {
                                width: 250,
                                height: 249 // this small hack fixes an issue with an unwanted border on Android
                            } }
                        />
                    </View>
                </Layout>

                <Layout style={ styles.walletInstructions }>
                    <Button onPress={ () => getData() } style={ styles.ctaButton }>
                        Refresh Data
                    </Button>
                </Layout>
            </Layout>

            {/* <Layout style={ styles.tapInstructions }>
                <Image
                    source={ require( './../img/tap-hint.png' ) }
                    resizeMode={ 'cover' }
                    // eslint-disable-next-line react-native/no-inline-styles
                    style={ {
                        width: 350,
                        height: 100 // this small hack fixes an issue with an unwanted border on Android
                    } }
                />
            </Layout> */}
        </>
    );
};
