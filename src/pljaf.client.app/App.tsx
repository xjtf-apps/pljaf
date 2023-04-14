import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Button, Text, TextInput, View } from 'react-native';

export default function App() {
  return (
    <View style={styles.container}>
      <Text>pljaf</Text>
      <TextInput style={{
          height: 40,
          width: 250,
          padding: 5,
          borderColor: 'gray',
          borderWidth: 1,
        }}
        keyboardType="phone-pad"
        placeholder="Unesi broj svog mobitela"
      />
      <Button title="Prijava" key="login" />
      <StatusBar style="auto" />
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
