import { StatusBar } from 'expo-status-bar';
import View from 'react-native-ui-lib/view';
import Text from 'react-native-ui-lib/text';
import Card from 'react-native-ui-lib/card';
import Button from 'react-native-ui-lib/button';
import TextField from 'react-native-ui-lib/textField';
import { StyleSheet } from 'react-native';

export default function App() {
  return (
    <View flex paddingH-25 paddingT-250>
      <Text foreground-primaryColor text20>Dobrodošli</Text>
      <TextField text50 placeholder="broj telefona" grey10 keyboardType="phone-pad"/>
      <View marginT-250>
        <Button text70 white background-secondaryColor label="Prijava"/>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
    alignItems: 'center',
    justifyContent: 'center',
  },
});
