import { mark } from './lib/perf'
import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'

mark('script-start')

const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
