import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import 'primeicons/primeicons.css'

// Часто используемые компоненты PrimeVue — регистрируем глобально.
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import MultiSelect from 'primevue/multiselect'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Toast from 'primevue/toast'
import Dialog from 'primevue/dialog'
import ToggleSwitch from 'primevue/toggleswitch'

import './style.css'
import App from './App.vue'
import router from './router'
import i18n from './i18n'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: { darkModeSelector: '.dark' },
  },
})
app.use(ToastService)
app.use(ConfirmationService)

app.component('PvButton', Button)
app.component('PvInputText', InputText)
app.component('PvTextarea', Textarea)
app.component('PvSelect', Select)
app.component('PvMultiSelect', MultiSelect)
app.component('PvDataTable', DataTable)
app.component('PvColumn', Column)
app.component('PvTag', Tag)
app.component('PvMessage', Message)
app.component('PvProgressSpinner', ProgressSpinner)
app.component('PvToast', Toast)
app.component('PvDialog', Dialog)
app.component('PvToggleSwitch', ToggleSwitch)

app.mount('#app')
