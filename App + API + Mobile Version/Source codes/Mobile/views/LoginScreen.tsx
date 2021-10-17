  
import { AppContext, UserInfo } from '../App';
import React, { useContext } from 'react';
import { Alert, StyleSheet, Text, TextInput, ToastAndroid, TouchableOpacity, View } from 'react-native';
import Environment from '../environment';
import AsyncStorage from '@react-native-community/async-storage';

export const LoginScreen = () => {

  const [username, setUsername] = React.useState( "" );
  const [password, setPassword] = React.useState( "" );

  const appData = useContext( AppContext );
  const { setView, storeUserInfo } = appData;

  const onLogin = async () => {

    console.log(Environment);
    console.log(username, password);

    const apiLogin = Environment.BASE_URL + "/Authenticate/login";
    const body = JSON.stringify({
      Username: username,
      Password: password,
    });
    console.log(apiLogin);

    fetch(apiLogin, {
        method: 'POST',
        headers:  {
          'Accept': 'application/json',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          username: username,
          password: password
        })
    })
    .then((response)=> {
      console.log(response);
      return response.json()
    })
    .then(async (res)=>{
      console.log('login success', res);
      if (res && res?.userId != null) {
        ToastAndroid.show("Login success", ToastAndroid.SHORT);
        storeUserInfo(res);
        console.log('set view LIST');
        setView( 'LIST' );
      } else {
        ToastAndroid.show("Login failed. Try again", ToastAndroid.SHORT);
      }
    })
    .catch((error) => {
      console.error(error);
    });
    
    // const testAPI = Environment.BASE_URL + "/WeatherForecast";
    // console.log(testAPI);
    // fetch(testAPI, {
    //   method: 'GET',
    // }).then((response) => {
    //   return response.json();
    // })
    // .then((responseJson) => {
    //     console.log(responseJson);
    // })
    // .catch((error) => {
    //   console.error(error);
    // });


    // fetch(apiLogin, {
    //   method: 'POST',
    //   headers: {
    //     Accept: 'application/json',
    //     'Content-Type': 'application/json',
    //   },
    //   body: body,
    // }).then((response) => {
    //   console.log(response);
    //   console.log(response.json());
    //   return response.json();
    // })
    // .then((responseJson) => {
    //     console.log(responseJson);
    // })
    // .catch((error) => {
    //   console.error(error);
    // });
  }

  return (

    <View style={styles.container}>
      <Text style={styles.logo}>VocaReminder</Text>
      <View style={styles.inputView} >
        <TextInput  
          style={styles.inputText}
          placeholder="Username..." 
          placeholderTextColor="#003f5c"
          onChangeText={text => setUsername(text)}
          />
      </View>
      <View style={styles.inputView} >
        <TextInput  
          secureTextEntry
          style={styles.inputText}
          placeholder="Password..." 
          placeholderTextColor="#003f5c"
          onChangeText={text => setPassword(text)}
          />
      </View>
      {/* <TouchableOpacity>
        <Text style={styles.forgot}>Forgot Password?</Text>
      </TouchableOpacity> */}
      <TouchableOpacity style={styles.loginBtn} onPress={onLogin}>
        <Text style={styles.loginText}>LOGIN</Text>
      </TouchableOpacity>
      {/* <TouchableOpacity>
        <Text style={styles.loginText}>Signup</Text>
      </TouchableOpacity> */}

    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#003f5c',
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo:{
    fontWeight:"bold",
    fontSize:50,
    color:"#fb5b5a",
    marginBottom:40
  },
  inputView:{
    width:"80%",
    backgroundColor:"#465881",
    borderRadius:25,
    height:50,
    marginBottom:20,
    justifyContent:"center",
    padding:20
  },
  inputText:{
    height:50,
    color:"white"
  },
  forgot:{
    color:"white",
    fontSize:11
  },
  loginBtn:{
    width:"80%",
    backgroundColor:"#fb5b5a",
    borderRadius:25,
    height:50,
    alignItems:"center",
    justifyContent:"center",
    marginTop:40,
    marginBottom:10
  },
  loginText:{
    color:"white"
  }
});